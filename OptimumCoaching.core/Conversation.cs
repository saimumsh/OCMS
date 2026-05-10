using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    // A messaging thread opened by a Student/Teacher to a target role
    // (CC or EC). Anyone in the target role can read and reply; once a
    // member of the role replies, the conversation continues with all
    // role members able to see the history.
    public class Conversation : AuditableEntity
    {
        [Required, MaxLength(200), Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        // The user who opened the thread (Student / Teacher).
        public Guid StartedByUserId { get; set; }
        public ApplicationUser? StartedByUser { get; set; }

        // Target audience role (e.g. "CC" or "EC"). Stored as the role
        // name string so it survives schema changes without an FK.
        [Required, MaxLength(20), Display(Name = "Recipient")]
        public string TargetRole { get; set; } = string.Empty;

        public ConversationStatus Status { get; set; } = ConversationStatus.Open;

        // Denormalized "latest activity" fields so the inbox can sort
        // and show unread state without scanning all messages.
        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
        public Guid? LastMessageSenderUserId { get; set; }

        public IList<Message> Messages { get; set; } = new List<Message>();
    }
}
