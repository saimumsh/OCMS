using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public interface IMessagingService
    {
        // Inbox: conversations the user can see (started by them OR targeted at
        // any role they hold). Each item carries a denormalized unread flag.
        Task<IList<InboxItem>> GetInboxAsync(Guid userId, IList<string> userRoles, int take = 50);

        Task<int> GetUnreadCountAsync(Guid userId, IList<string> userRoles);

        // Loads a conversation with all messages eagerly, plus an access check.
        // Returns null when the user is not allowed to see it.
        Task<Conversation?> GetByIdForUserAsync(Guid conversationId, Guid userId, IList<string> userRoles);

        Task<(bool Success, string Message, Conversation? Conversation)> CreateAsync(
            Guid initiatorUserId, string targetRole, string subject, string body);

        Task<(bool Success, string Message, Message? Reply)> ReplyAsync(
            Guid conversationId, Guid senderUserId, IList<string> senderRoles, string body);

        // Stamps the user's read marker for the conversation.
        Task MarkReadAsync(Guid conversationId, Guid userId);
    }

    // Lightweight projection used by the inbox view — avoids lazy-loading
    // every Conversation just to render a row.
    public class InboxItem
    {
        public Guid Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string TargetRole { get; set; } = string.Empty;
        public ConversationStatus Status { get; set; }
        public DateTime LastMessageAt { get; set; }
        public string? LastMessageSnippet { get; set; }
        public string? LastSenderName { get; set; }
        public string OriginatorName { get; set; } = string.Empty;
        public Guid OriginatorUserId { get; set; }
        public bool IsUnread { get; set; }

        // True when the current viewer is the originator (Student/Teacher).
        // Used by the view to flip the "to/from" labels.
        public bool ViewerIsOriginator { get; set; }
    }
}
