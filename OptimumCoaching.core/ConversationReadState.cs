namespace OptimumCoaching.core
{
    // Per-user "I have read up to" marker for a Conversation. Used to
    // compute unread counts in inboxes — both for the originator and
    // for individual members of the target role.
    public class ConversationReadState
    {
        public Guid ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;

        public Guid UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public DateTime LastReadAt { get; set; } = DateTime.UtcNow;
    }
}
