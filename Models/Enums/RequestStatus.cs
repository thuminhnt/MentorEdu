namespace MentorEdu.Models.Enums
{
    public enum RequestStatus
    {
        Open,       // Initial state - waiting for mentor response
        Accepted,   // Mentor accepted the request
        Rejected,   // Mentor rejected the request
        Matched,    // Legacy - used for proposal matching
        InOrder,    // Request has an active order
        Closed,     // Closed by student
        Deleted     // Soft deleted
    }
}
