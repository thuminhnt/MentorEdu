using MentorEdu.DTOs.Groups;
using MentorEdu.Services.Common;

namespace MentorEdu.Services.Interfaces
{
    public interface IGroupService
    {
        Task<ServiceResult<GroupDto>> CreateGroupAsync(long mentorId, CreateGroupDto dto);
        Task<ServiceResult<GroupDto>> JoinGroupAsync(long userId, string inviteCode);
        Task<ServiceResult<GroupDto>> GetGroupByIdAsync(long groupId);
        Task<ServiceResult<List<GroupDto>>> GetGroupsForUserAsync(long userId);
        Task<ServiceResult<GroupMessageDto>> SendGroupMessageAsync(long senderId, SendGroupMessageDto dto);
        Task<ServiceResult<List<GroupMessageDto>>> GetGroupMessagesAsync(long groupId, long userId, int pageNumber = 1, int pageSize = 50);
        Task<ServiceResult<bool>> UserHasAccessToGroupAsync(long userId, long groupId);
        Task<ServiceResult<bool>> LeaveGroupAsync(long userId, long groupId);
    }
}
