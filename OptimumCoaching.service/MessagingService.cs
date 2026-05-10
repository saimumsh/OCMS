using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;

namespace OptimumCoaching.service
{
    public class MessagingService : IMessagingService
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _uow;

        public MessagingService(ApplicationDbContext db, IUnitOfWork uow)
        {
            _db = db;
            _uow = uow;
        }

        public async Task<IList<InboxItem>> GetInboxAsync(
            Guid userId, IList<string> userRoles, int take = 50)
        {
            var roles = userRoles ?? new List<string>();

            // A user sees a conversation when they started it OR when they
            // hold the target role. Any role membership counts (an Admin who
            // also has CC sees CC inboxes).
            var query = _db.Conversations
                .Include(c => c.StartedByUser)
                .Where(c => c.StartedByUserId == userId || roles.Contains(c.TargetRole))
                .OrderByDescending(c => c.LastMessageAt)
                .Take(take);

            var conversations = await query.ToListAsync();
            var convIds = conversations.Select(c => c.Id).ToList();

            // Pull this user's read markers for the listed conversations in
            // one round trip.
            var readMap = await _db.ConversationReadStates
                .Where(rs => rs.UserId == userId && convIds.Contains(rs.ConversationId))
                .ToDictionaryAsync(rs => rs.ConversationId, rs => rs.LastReadAt);

            // Pre-load last message snippet + sender name for each. EF Core
            // doesn't allow Include() after a Select() projection, so we do
            // this in two passes: first find the latest message id per
            // conversation, then load those rows with their navigations.
            var lastMessageIds = await _db.Messages
                .Where(m => convIds.Contains(m.ConversationId))
                .GroupBy(m => m.ConversationId)
                .Select(g => g.OrderByDescending(m => m.SentAt).First().Id)
                .ToListAsync();

            var lastMessages = await _db.Messages
                .Include(m => m.SenderUser)
                .Where(m => lastMessageIds.Contains(m.Id))
                .ToListAsync();

            var snippetMap = lastMessages.ToDictionary(m => m.ConversationId, m => m);

            return conversations.Select(c =>
            {
                var lastMsg = snippetMap.TryGetValue(c.Id, out var lm) ? lm : null;
                var lastReadAt = readMap.TryGetValue(c.Id, out var t) ? t : (DateTime?)null;
                var unread =
                    c.LastMessageSenderUserId.HasValue &&
                    c.LastMessageSenderUserId.Value != userId &&
                    (!lastReadAt.HasValue || lastReadAt.Value < c.LastMessageAt);

                return new InboxItem
                {
                    Id = c.Id,
                    Subject = c.Subject,
                    TargetRole = c.TargetRole,
                    Status = c.Status,
                    LastMessageAt = c.LastMessageAt,
                    LastMessageSnippet = lastMsg?.Body is { Length: > 0 } body
                        ? (body.Length > 140 ? body[..140] + "…" : body)
                        : null,
                    LastSenderName = lastMsg?.SenderUser?.FullName,
                    OriginatorName = c.StartedByUser?.FullName ?? "Unknown",
                    OriginatorUserId = c.StartedByUserId,
                    IsUnread = unread,
                    ViewerIsOriginator = c.StartedByUserId == userId
                };
            }).ToList();
        }

        public async Task<int> GetUnreadCountAsync(Guid userId, IList<string> userRoles)
        {
            var roles = userRoles ?? new List<string>();

            // Conversations visible to this user where the latest message
            // came from someone else and they haven't caught up to it.
            return await _db.Conversations
                .Where(c => c.StartedByUserId == userId || roles.Contains(c.TargetRole))
                .Where(c => c.LastMessageSenderUserId.HasValue && c.LastMessageSenderUserId != userId)
                .Where(c => !_db.ConversationReadStates.Any(rs =>
                    rs.ConversationId == c.Id &&
                    rs.UserId == userId &&
                    rs.LastReadAt >= c.LastMessageAt))
                .CountAsync();
        }

        public async Task<Conversation?> GetByIdForUserAsync(
            Guid conversationId, Guid userId, IList<string> userRoles)
        {
            var roles = userRoles ?? new List<string>();
            var conv = await _db.Conversations
                .Include(c => c.StartedByUser)
                .Include(c => c.Messages.OrderBy(m => m.SentAt))
                    .ThenInclude(m => m.SenderUser)
                .FirstOrDefaultAsync(c => c.Id == conversationId);
            if (conv == null) return null;

            // Access check — same rules as the inbox query.
            if (conv.StartedByUserId != userId && !roles.Contains(conv.TargetRole))
                return null;
            return conv;
        }

        public async Task<(bool Success, string Message, Conversation? Conversation)> CreateAsync(
            Guid initiatorUserId, string targetRole, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(subject))
                return (false, "Subject is required", null);
            if (string.IsNullOrWhiteSpace(body))
                return (false, "Message is required", null);
            if (!Roles.MessageableRoles.Contains(targetRole))
                return (false, "Invalid recipient role", null);

            var now = DateTime.UtcNow;
            var conv = new Conversation
            {
                Id = Guid.NewGuid(),
                Subject = subject.Trim(),
                StartedByUserId = initiatorUserId,
                TargetRole = targetRole,
                Status = ConversationStatus.Open,
                LastMessageAt = now,
                LastMessageSenderUserId = initiatorUserId,
                IsActive = true,
                Created = now,
                CreatedBy = initiatorUserId
            };

            var firstMessage = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conv.Id,
                SenderUserId = initiatorUserId,
                Body = body,
                SentAt = now,
                IsActive = true,
                Created = now,
                CreatedBy = initiatorUserId
            };

            _db.Conversations.Add(conv);
            _db.Messages.Add(firstMessage);

            // Originator has implicitly read their own message.
            _db.ConversationReadStates.Add(new ConversationReadState
            {
                ConversationId = conv.Id,
                UserId = initiatorUserId,
                LastReadAt = now
            });

            await _uow.CompleteAsync();
            return (true, "Message sent", conv);
        }

        public async Task<(bool Success, string Message, Message? Reply)> ReplyAsync(
            Guid conversationId, Guid senderUserId, IList<string> senderRoles, string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return (false, "Message is required", null);

            var conv = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId);
            if (conv == null) return (false, "Conversation not found", null);
            if (conv.Status == ConversationStatus.Closed)
                return (false, "Conversation is closed", null);

            var roles = senderRoles ?? new List<string>();
            if (conv.StartedByUserId != senderUserId && !roles.Contains(conv.TargetRole))
                return (false, "You are not a participant in this conversation", null);

            var now = DateTime.UtcNow;
            var msg = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conv.Id,
                SenderUserId = senderUserId,
                Body = body,
                SentAt = now,
                IsActive = true,
                Created = now,
                CreatedBy = senderUserId
            };

            conv.LastMessageAt = now;
            conv.LastMessageSenderUserId = senderUserId;
            conv.LastModified = now;
            conv.LastModifiedBy = senderUserId;

            _db.Messages.Add(msg);

            // Sender has read their own latest message — upsert their marker.
            await UpsertReadStateAsync(conv.Id, senderUserId, now);

            await _uow.CompleteAsync();
            return (true, "Reply sent", msg);
        }

        public async Task MarkReadAsync(Guid conversationId, Guid userId)
        {
            await UpsertReadStateAsync(conversationId, userId, DateTime.UtcNow);
            await _uow.CompleteAsync();
        }

        private async Task UpsertReadStateAsync(Guid conversationId, Guid userId, DateTime when)
        {
            var existing = await _db.ConversationReadStates
                .FirstOrDefaultAsync(rs => rs.ConversationId == conversationId && rs.UserId == userId);
            if (existing == null)
            {
                _db.ConversationReadStates.Add(new ConversationReadState
                {
                    ConversationId = conversationId,
                    UserId = userId,
                    LastReadAt = when
                });
            }
            else if (existing.LastReadAt < when)
            {
                existing.LastReadAt = when;
            }
        }
    }
}
