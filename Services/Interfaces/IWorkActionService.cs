using MentorEdu.DTOs.Chat;

namespace MentorEdu.Services.Interfaces
{
    /// <summary>
    /// Handles all work-action business logic: validating requests, managing pending-action state,
    /// executing DB operations (start/pause/end session, complete order) and producing results the Hub broadcasts.
    /// </summary>
    public interface IWorkActionService
    {
        /// <summary>
        /// Initiate a "Start Work" request.
        /// Validates conversation access and participant match, creates a pending action.
        /// </summary>
        Task<WorkActionPendingResult> RequestStartWorkAsync(
            long requesterId, long conversationId, long orderId, long mentorId, long studentId);

        /// <summary>
        /// Initiate a "Pause Work" request (delegates to RequestWorkActionAsync internally).
        /// </summary>
        Task<WorkActionPendingResult> RequestPauseWorkAsync(long requesterId, long conversationId, long sessionId);

        /// <summary>
        /// Initiate an "End Work" request (delegates to RequestWorkActionAsync internally).
        /// </summary>
        Task<WorkActionPendingResult> RequestEndWorkAsync(long requesterId, long conversationId, long sessionId);

        /// <summary>
        /// Initiate a "Complete Order" request.
        /// Sends a system chat message and a push notification to the other party.
        /// Result.SystemMessageDto contains the chat message DTO that the Hub should broadcast.
        /// </summary>
        Task<WorkActionPendingResult> RequestCompleteOrderAsync(long requesterId, long conversationId, long orderId);

        /// <summary>
        /// Accept or reject a pending work action.
        /// On accept: executes the appropriate DB operation and returns session/order data for broadcasting.
        /// On reject: removes the action from pending state.
        /// </summary>
        Task<WorkActionResponseResult> RespondWorkActionAsync(string requestId, long userId, bool accept);

        /// <summary>Returns all pending actions targeting the given user in the given conversation.</summary>
        IEnumerable<PendingWorkActionState> GetPendingActionsForUser(long userId, long conversationId);
    }
}
