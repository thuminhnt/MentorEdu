namespace MentorEdu.Models.Enums
{
    public enum NotificationType
    {
        System,
        RequestCreated,
        RequestAssigned,
        ProposalCreated,
        ProposalAccepted,
        OrderStarted,
        OrderCompleted,
        OrderDisputed,
        WalletTransaction,
        RelevantPost  // New: notify mentor about relevant community posts
    }
}
