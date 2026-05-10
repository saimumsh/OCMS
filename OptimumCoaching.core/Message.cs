using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    // A single post inside a Conversation thread.
    public class Message : AuditableEntity
    {
        public Guid ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;

        public Guid SenderUserId { get; set; }
        public ApplicationUser? SenderUser { get; set; }

        [Required, MaxLength(4000), Display(Name = "Message")]
        public string Body { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
