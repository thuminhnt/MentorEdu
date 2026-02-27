namespace MentorEdu.Models.Enums
{
    /// <summary>
    /// Actions that can be performed on a mentor application
    /// Used for tracking history and ML training data
    /// </summary>
    public enum ApplicationAction
    {
        Submitted = 0,   // User submitted/resubmitted application
        Approved = 1,    // Admin approved the application
        Rejected = 2,    // Admin rejected the application
        AutoApproved = 3, // AI auto-approved (future)
        AutoRejected = 4, // AI auto-rejected (future)
        AiReEvaluated = 5 // Admin triggered AI re-evaluation
    }
}
