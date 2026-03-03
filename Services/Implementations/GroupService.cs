using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Groups;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using MentorEdu.Services.Common;
using MentorEdu.Services.Interfaces;


namespace MentorEdu.Services.Implementations
{
    public class GroupService : BaseService, IGroupService
    {
        private readonly IGroupRepository _groupRepository;
        private readonly IGroupMemberRepository _groupMemberRepository;
        private readonly IGroupMessageRepository _groupMessageRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GroupService> _logger;

        public GroupService(
            IGroupRepository groupRepository,
            IGroupMemberRepository groupMemberRepository,
            IGroupMessageRepository groupMessageRepository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            ILogger<GroupService> logger) : base(unitOfWork)
        {
            _groupRepository = groupRepository;
            _groupMemberRepository = groupMemberRepository;
            _groupMessageRepository = groupMessageRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<ServiceResult<GroupDto>> CreateGroupAsync(long mentorId, CreateGroupDto dto)
        {
            // Kiểm tra mentor có tồn tại không
            var mentor = await _userRepository.GetByIdWithProfilesAsync(mentorId);
            if (mentor == null)
                return ServiceResult<GroupDto>.NotFoundResult("Mentor not found");

            // Tạo mã mời tham gia độc nhất
            var inviteCode = await GenerateUniqueInviteCodeAsync();

            var group = new Group
            {
                Name = dto.Name,
                Description = dto.Description,
                MentorId = mentorId,
                InviteCode = inviteCode,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            // Thêm mentor vào nhóm làm admin qua navigation property — EF Core tự động xử lý FK (GroupId)
            var mentorMember = new GroupMember
            {
                UserId = mentorId,
                Role = GroupRole.Admin,
                JoinedAt = DateTime.UtcNow
            };
            group.Members.Add(mentorMember);

            await _groupRepository.AddAsync(group);
            await _unitOfWork.SaveChangesAsync();

            // Gắn các navigation property để tránh trập vào DB thêm lần nữa
            group.Mentor = mentor;
            mentorMember.User = mentor;
            group.Members = new List<GroupMember> { mentorMember };

            var groupDto = await MapToGroupDtoAsync(group, mentorId);
            return ServiceResult<GroupDto>.SuccessResult(groupDto);
        }

        public async Task<ServiceResult<GroupDto>> JoinGroupAsync(long userId, string inviteCode)
        {
            var group = await _groupRepository.GetByInviteCodeAsync(inviteCode);
            if (group == null)
                return ServiceResult<GroupDto>.FailResult("Invalid invite code");

            // Kiểm tra đã là thành viên chưa
            var existingMember = await _groupMemberRepository.GetMemberAsync(group.Id, userId);
            if (existingMember != null)
                return ServiceResult<GroupDto>.FailResult("You are already a member of this group");

            // Lấy thông tin người dùng cho tin nhắn hệ thống (gia nhập)
            var user = await _userRepository.GetByIdWithProfilesAsync(userId);
            var userName = user?.UserProfile?.FullName ?? user?.Email ?? "Someone";

            var member = new GroupMember
            {
                GroupId = group.Id,
                UserId = userId,
                Role = GroupRole.Member,
                JoinedAt = DateTime.UtcNow
            };

            await _groupMemberRepository.AddAsync(member);

            // Tạo tin nhắn hệ thống khi có thành viên mới gia nhập
            var systemMessage = new GroupMessage
            {
                GroupId = group.Id,
                SenderId = userId,
                Content = $"{userName} joined the group",
                MessageType = MessageType.System,
                CreatedAt = DateTime.UtcNow
            };
            await _groupMessageRepository.AddAsync(systemMessage);

            await _unitOfWork.SaveChangesAsync();

            // Tải lại nhóm sau khi cập nhật danh sách thành viên (JoinGroup)
            var updatedGroup = await _groupRepository.GetByIdAsync(group.Id);
            if (updatedGroup == null)
                return ServiceResult<GroupDto>.FailResult("Failed to retrieve group");

            var groupDto = await MapToGroupDtoAsync(updatedGroup, userId);
            return ServiceResult<GroupDto>.SuccessResult(groupDto);
        }

        public async Task<ServiceResult<GroupDto>> AddMemberAsync(long adminId, long groupId, long userId)
        {
            var group = await _groupRepository.GetByIdAsync(groupId);
            if (group == null)
                return ServiceResult<GroupDto>.NotFoundResult("Group not found");

            // Kiểm tra admin có quyền thực hiện không
            var adminMember = await _groupMemberRepository.GetMemberAsync(groupId, adminId);
            if (adminMember == null || adminMember.Role != GroupRole.Admin)
                return ServiceResult<GroupDto>.FailResult("Only group admin can add members");

            // Kiểm tra người dùng đã là thành viên chưa (AddMember)
            var existingMember = await _groupMemberRepository.GetMemberAsync(groupId, userId);
            if (existingMember != null)
                return ServiceResult<GroupDto>.FailResult("User is already a member of this group");

            // Lấy thông tin người dùng cho tin nhắn hệ thống (thêm thành viên)
            var user = await _userRepository.GetByIdWithProfilesAsync(userId);
            if (user == null)
                return ServiceResult<GroupDto>.NotFoundResult("User not found");

            var userName = user.UserProfile?.FullName ?? user.Email ?? "Someone";

            // Thêm thành viên mới vào nhóm
            var member = new GroupMember
            {
                GroupId = groupId,
                UserId = userId,
                Role = GroupRole.Member,
                JoinedAt = DateTime.UtcNow
            };
            await _groupMemberRepository.AddAsync(member);

            // Tạo tin nhắn hệ thống khi admin thêm thành viên
            var systemMessage = new GroupMessage
            {
                GroupId = groupId,
                SenderId = adminId,
                Content = $"{userName} was added to the group",
                MessageType = MessageType.System,
                CreatedAt = DateTime.UtcNow
            };
            await _groupMessageRepository.AddAsync(systemMessage);

            // Notify the added user
            var adminUser = await _userRepository.GetByIdWithProfilesAsync(adminId);
            var adminName = adminUser?.UserProfile?.FullName ?? adminUser?.Email ?? "The group admin";
            var notification = new Notification
            {
                UserId = userId,
                Type = NotificationType.System,
                Title = "You've been added to a group",
                Body = $"{adminName} has added you to the group \"{group.Name}\".",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Notifications.AddAsync(notification);

            await _unitOfWork.SaveChangesAsync();

            // Tải lại nhóm sau khi cập nhật (AddMember)
            var updatedGroup = await _groupRepository.GetByIdAsync(groupId);
            if (updatedGroup == null)
                return ServiceResult<GroupDto>.FailResult("Failed to retrieve group");

            var groupDto = await MapToGroupDtoAsync(updatedGroup, adminId);
            return ServiceResult<GroupDto>.SuccessResult(groupDto);
        }

        public async Task<ServiceResult<bool>> RemoveMemberAsync(long adminId, long groupId, long userId)
        {
            var group = await _groupRepository.GetByIdAsync(groupId);
            if (group == null)
                return ServiceResult<bool>.NotFoundResult("Group not found");

            // Only admin can kick members
            var adminMember = await _groupMemberRepository.GetMemberAsync(groupId, adminId);
            if (adminMember == null || adminMember.Role != GroupRole.Admin)
                return ServiceResult<bool>.FailResult("Only the group admin can remove members");

            // Cannot kick yourself (admin)
            if (adminId == userId)
                return ServiceResult<bool>.FailResult("Admin cannot remove themselves from the group");

            var targetMember = await _groupMemberRepository.GetMemberAsync(groupId, userId);
            if (targetMember == null)
                return ServiceResult<bool>.FailResult("User is not a member of this group");

            var targetUser = await _userRepository.GetByIdWithProfilesAsync(userId);
            var userName = targetUser?.UserProfile?.FullName ?? targetUser?.Email ?? "A member";

            // System message
            var systemMessage = new GroupMessage
            {
                GroupId = groupId,
                SenderId = adminId,
                Content = $"{userName} was removed from the group",
                MessageType = MessageType.System,
                CreatedAt = DateTime.UtcNow
            };
            await _groupMessageRepository.AddAsync(systemMessage);

            await _groupMemberRepository.DeleteAsync(targetMember);

            // Notify the removed user
            var adminUser = await _userRepository.GetByIdWithProfilesAsync(adminId);
            var adminName = adminUser?.UserProfile?.FullName ?? adminUser?.Email ?? "The group admin";
            var notification = new Notification
            {
                UserId = userId,
                Type = NotificationType.System,
                Title = "You were removed from a group",
                Body = $"{adminName} has removed you from the group \"{group.Name}\".",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Notifications.AddAsync(notification);

            await _unitOfWork.SaveChangesAsync();
            return ServiceResult<bool>.SuccessResult(true);
        }

        public async Task<ServiceResult<GroupDto>> GetGroupByIdAsync(long groupId)
        {
            var group = await _groupRepository.GetByIdAsync(groupId);
            if (group == null)
                return ServiceResult<GroupDto>.NotFoundResult("Group not found");

            var groupDto = await MapToGroupDtoAsync(group);
            return ServiceResult<GroupDto>.SuccessResult(groupDto);
        }

        public async Task<ServiceResult<List<GroupDto>>> GetGroupsForUserAsync(long userId)
        {
            var groups = await _groupRepository.GetGroupsByUserIdAsync(userId);
            var groupDtos = new List<GroupDto>();
            foreach (var g in groups)
                groupDtos.Add(await MapToGroupDtoAsync(g, userId));

            return ServiceResult<List<GroupDto>>.SuccessResult(groupDtos);
        }

        public async Task<ServiceResult<GroupMessageDto>> SendGroupMessageAsync(long senderId, SendGroupMessageDto dto)
        {
            // Kiểm tra người dùng có là thành viên không (gửi tin nhắn)
            var isMember = await _groupMemberRepository.IsMemberAsync(dto.GroupId, senderId);
            if (!isMember)
                return ServiceResult<GroupMessageDto>.FailResult("You are not a member of this group");

            var message = new GroupMessage
            {
                GroupId = dto.GroupId,
                SenderId = senderId,
                Content = dto.Content,
                MessageType = dto.MessageType,
                CreatedAt = DateTime.UtcNow
            };

            await _groupMessageRepository.AddAsync(message);
            await _unitOfWork.SaveChangesAsync();

            // Tải lại để lấy navigation properties (thông tin người gửi)
            var savedMessage = await _groupMessageRepository.GetByIdWithSenderAsync(message.Id);
            if (savedMessage == null)
                return ServiceResult<GroupMessageDto>.FailResult("Failed to retrieve sent message");

            var messageDto = MapToGroupMessageDto(savedMessage);
            return ServiceResult<GroupMessageDto>.SuccessResult(messageDto);
        }

        public async Task<ServiceResult<object>> GetGroupMessagesAsync(long groupId, long userId, int pageNumber = 1, int pageSize = 50)
        {
            // Kiểm tra quyền truy cập của người dùng vào nhóm
            var isMember = await _groupMemberRepository.IsMemberAsync(groupId, userId);
            if (!isMember)
                return ServiceResult<object>.FailResult("You do not have access to this group");

            var totalCount = await _groupMessageRepository.GetMessageCountAsync(groupId);
            var messages = await _groupMessageRepository.GetMessagesByGroupIdAsync(groupId, pageNumber, pageSize);
            var messageDtos = messages.Select(MapToGroupMessageDto).ToList();

            var result = new
            {
                Messages = messageDtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                HasMore = (pageNumber * pageSize) < totalCount
            };

            return ServiceResult<object>.SuccessResult(result);
        }

        public async Task<ServiceResult<bool>> UserHasAccessToGroupAsync(long userId, long groupId)
        {
            var isMember = await _groupMemberRepository.IsMemberAsync(groupId, userId);
            return ServiceResult<bool>.SuccessResult(isMember);
        }

        public async Task<ServiceResult<bool>> LeaveGroupAsync(long userId, long groupId)
        {
            var member = await _groupMemberRepository.GetMemberAsync(groupId, userId);
            if (member == null)
                return ServiceResult<bool>.FailResult("You are not a member of this group");

            // Ngăn admin (mentor) rời khỏi nhóm
            if (member.Role == GroupRole.Admin)
                return ServiceResult<bool>.FailResult("Group admin cannot leave the group");

            // Lấy tên người dùng cho tin nhắn hệ thống (rời nhóm)
            var userName = member.User?.UserProfile?.FullName ?? member.User?.Email ?? "Unknown";

            // Tạo tin nhắn hệ thống trước khi xóa thành viên
            var systemMessage = new GroupMessage
            {
                GroupId = groupId,
                SenderId = userId,
                Content = $"{userName} left the group",
                MessageType = MessageType.System,
                CreatedAt = DateTime.UtcNow
            };
            await _groupMessageRepository.AddAsync(systemMessage);

            await _groupMemberRepository.DeleteAsync(member);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<bool>.SuccessResult(true);
        }

        #region Private Helper Methods

        public async Task MarkGroupAsReadAsync(long groupId, long userId)
        {
            await _groupMemberRepository.UpdateLastReadAtAsync(groupId, userId);
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task<string> GenerateUniqueInviteCodeAsync()
        {
            string inviteCode;
            bool isUnique;

            do
            {
                inviteCode = GenerateRandomCode(8);
                isUnique = await _groupRepository.IsInviteCodeUniqueAsync(inviteCode);
            } while (!isUnique);

            return inviteCode;
        }

        private static string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
        }

        private async Task<GroupDto> MapToGroupDtoAsync(Group group, long? userId = null)
        {
            // Lấy tin nhắn mới nhất để hiển thị preview
            var latestMessage = await _groupMessageRepository.GetLatestMessageByGroupIdAsync(group.Id);
            
            // Định dạng hiển thị tin nhắn cuối dựa theo loại tin nhắn
            string? displayMessage = latestMessage?.Content;
            if (latestMessage != null)
            {
                if (latestMessage.MessageType == MentorEdu.Models.Enums.MessageType.Image)
                {
                    displayMessage = "Photo";
                }
                else if (latestMessage.MessageType == MentorEdu.Models.Enums.MessageType.File)
                {
                    displayMessage = "File";
                }
            }

            // Tính số tin nhắn chưa đọc nếu có userId
            int unreadCount = 0;
            if (userId.HasValue)
            {
                var member = group.Members?.FirstOrDefault(m => m.UserId == userId.Value);
                if (member != null)
                {
                    unreadCount = await _groupMessageRepository.GetUnreadCountAsync(group.Id, userId.Value, member.LastReadAt);
                }
            }
            
            return new GroupDto
            {
                Id = group.Id,
                Name = group.Name ?? string.Empty,
                Description = group.Description,
                MentorId = group.MentorId,
                MentorName = group.Mentor?.UserProfile?.FullName ?? group.Mentor?.Email ?? "Unknown",
                InviteCode = group.InviteCode ?? string.Empty,
                CreatedAt = group.CreatedAt,
                MemberCount = group.Members?.Count ?? 0,
                Members = group.Members?.Select(m => new GroupMemberDto
                {
                    UserId = m.UserId,
                    UserName = m.User?.UserProfile?.FullName ?? m.User?.Email ?? "Unknown",
                    AvatarUrl = m.User?.UserProfile?.AvatarUrl,
                    Role = m.Role.ToString(),
                    JoinedAt = m.JoinedAt
                }).ToList() ?? new List<GroupMemberDto>(),
                LastMessage = displayMessage,
                LastMessageSender = latestMessage?.Sender?.UserProfile?.FullName ?? latestMessage?.Sender?.Email,
                LastMessageAt = latestMessage?.CreatedAt,
                UnreadCount = unreadCount
            };
        }

        private GroupMessageDto MapToGroupMessageDto(GroupMessage message)
        {
            return new GroupMessageDto
            {
                Id = message.Id,
                GroupId = message.GroupId,
                SenderId = message.SenderId,
                SenderName = message.Sender?.UserProfile?.FullName ?? message.Sender?.Email ?? "Unknown",
                SenderAvatar = message.Sender?.UserProfile?.AvatarUrl,
                Content = message.Content,
                MessageType = message.MessageType.ToString(),
                CreatedAt = message.CreatedAt
            };
        }

        #endregion
    }
}
