using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Groups;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using MentorEdu.Services.Common;
using MentorEdu.Services.Interfaces;


namespace MentorEdu.Services.Implementations
{
    public class GroupService : IGroupService
    {
        private readonly IGroupRepository _groupRepository;
        private readonly IGroupMemberRepository _groupMemberRepository;
        private readonly IGroupMessageRepository _groupMessageRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public GroupService(
            IGroupRepository groupRepository,
            IGroupMemberRepository groupMemberRepository,
            IGroupMessageRepository groupMessageRepository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork)
        {
            _groupRepository = groupRepository;
            _groupMemberRepository = groupMemberRepository;
            _groupMessageRepository = groupMessageRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<GroupDto>> CreateGroupAsync(long mentorId, CreateGroupDto dto)
        {
            try
            {
                // Verify mentor exists
                var mentor = await _userRepository.GetByIdWithProfilesAsync(mentorId);
                if (mentor == null)
                {
                    return ServiceResult<GroupDto>.Failure("Mentor not found");
                }

                // Generate unique invite code
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

                // Add mentor as admin member directly to collection
                // EF Core will handle the FK (GroupId) automatically
                var mentorMember = new GroupMember
                {
                    // GroupId NOT set here, handled by navigation
                    UserId = mentorId,
                    Role = GroupRole.Admin,
                    JoinedAt = DateTime.UtcNow
                };
                group.Members.Add(mentorMember);

                await _groupRepository.AddAsync(group);
                // await _groupMemberRepository.AddAsync(mentorMember); // REMOVED: Managed by navigation
                await _unitOfWork.SaveChangesAsync();

                // Manually attach properties to avoid reloading from DB
                // This ensures we have the necessary navigation properties for mapping
                group.Mentor = mentor;
                // Note: mentorMember.User setter might be needed if not virtual, but assuming EF Core POCO
                mentorMember.User = mentor;
                group.Members = new List<GroupMember> { mentorMember };

                var groupDto = MapToGroupDto(group);
                return ServiceResult<GroupDto>.SuccessResult(groupDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<GroupDto>.Failure($"Error creating group: {ex.Message}");
            }
        }

        public async Task<ServiceResult<GroupDto>> JoinGroupAsync(long userId, string inviteCode)
        {
            try
            {
                var group = await _groupRepository.GetByInviteCodeAsync(inviteCode);
                if (group == null)
                {
                    return ServiceResult<GroupDto>.Failure("Invalid invite code");
                }

                // Check if already a member
                var existingMember = await _groupMemberRepository.GetMemberAsync(group.Id, userId);
                if (existingMember != null)
                {
                    return ServiceResult<GroupDto>.Failure("You are already a member of this group");
                }

                var member = new GroupMember
                {
                    GroupId = group.Id,
                    UserId = userId,
                    Role = GroupRole.Member,
                    JoinedAt = DateTime.UtcNow
                };

                await _groupMemberRepository.AddAsync(member);
                await _unitOfWork.SaveChangesAsync();

                // Reload group with updated members
                var updatedGroup = await _groupRepository.GetByIdAsync(group.Id);
                if (updatedGroup == null)
                {
                    return ServiceResult<GroupDto>.Failure("Failed to retrieve group");
                }

                var groupDto = MapToGroupDto(updatedGroup);
                return ServiceResult<GroupDto>.SuccessResult(groupDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<GroupDto>.Failure($"Error joining group: {ex.Message}");
            }
        }

        public async Task<ServiceResult<GroupDto>> GetGroupByIdAsync(long groupId)
        {
            try
            {
                var group = await _groupRepository.GetByIdAsync(groupId);
                if (group == null)
                {
                    return ServiceResult<GroupDto>.Failure("Group not found");
                }

                var groupDto = MapToGroupDto(group);
                return ServiceResult<GroupDto>.SuccessResult(groupDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<GroupDto>.Failure($"Error retrieving group: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<GroupDto>>> GetGroupsForUserAsync(long userId)
        {
            try
            {
                var groups = await _groupRepository.GetGroupsByUserIdAsync(userId);
                var groupDtos = groups.Select(MapToGroupDto).ToList();
                return ServiceResult<List<GroupDto>>.SuccessResult(groupDtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<GroupDto>>.Failure($"Error retrieving groups: {ex.Message}");
            }
        }

        public async Task<ServiceResult<GroupMessageDto>> SendGroupMessageAsync(long senderId, SendGroupMessageDto dto)
        {
            try
            {
                // Verify user is a member
                var isMember = await _groupMemberRepository.IsMemberAsync(dto.GroupId, senderId);
                if (!isMember)
                {
                    return ServiceResult<GroupMessageDto>.Failure("You are not a member of this group");
                }

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

                // Reload to get navigation properties
                var savedMessage = await _groupMessageRepository.GetByIdAsync(message.Id);
                if (savedMessage == null)
                {
                    return ServiceResult<GroupMessageDto>.Failure("Failed to retrieve sent message");
                }

                var messageDto = MapToGroupMessageDto(savedMessage);
                return ServiceResult<GroupMessageDto>.SuccessResult(messageDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<GroupMessageDto>.Failure($"Error sending message: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<GroupMessageDto>>> GetGroupMessagesAsync(long groupId, long userId, int pageNumber = 1, int pageSize = 50)
        {
            try
            {
                // Verify user has access
                var isMember = await _groupMemberRepository.IsMemberAsync(groupId, userId);
                if (!isMember)
                {
                    return ServiceResult<List<GroupMessageDto>>.Failure("You do not have access to this group");
                }

                var messages = await _groupMessageRepository.GetMessagesByGroupIdAsync(groupId, pageNumber, pageSize);
                var messageDtos = messages.Select(MapToGroupMessageDto).ToList();
                return ServiceResult<List<GroupMessageDto>>.SuccessResult(messageDtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<GroupMessageDto>>.Failure($"Error retrieving messages: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> UserHasAccessToGroupAsync(long userId, long groupId)
        {
            try
            {
                var isMember = await _groupMemberRepository.IsMemberAsync(groupId, userId);
                return ServiceResult<bool>.SuccessResult(isMember);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"Error checking access: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> LeaveGroupAsync(long userId, long groupId)
        {
            try
            {
                var member = await _groupMemberRepository.GetMemberAsync(groupId, userId);
                if (member == null)
                {
                    return ServiceResult<bool>.Failure("You are not a member of this group");
                }

                // Prevent admin (mentor) from leaving
                if (member.Role == GroupRole.Admin)
                {
                    return ServiceResult<bool>.Failure("Group admin cannot leave the group");
                }

                await _groupMemberRepository.DeleteAsync(member);
                await _unitOfWork.SaveChangesAsync();

                return ServiceResult<bool>.SuccessResult(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"Error leaving group: {ex.Message}");
            }
        }

        #region Private Helper Methods

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

        private string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private GroupDto MapToGroupDto(Group group)
        {
            return new GroupDto
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                MentorId = group.MentorId,
                MentorName = group.Mentor?.UserProfile?.FullName ?? group.Mentor?.Email ?? "Unknown",
                InviteCode = group.InviteCode,
                CreatedAt = group.CreatedAt,
                MemberCount = group.Members?.Count ?? 0,
                Members = group.Members?.Select(m => new GroupMemberDto
                {
                    UserId = m.UserId,
                    UserName = m.User?.UserProfile?.FullName ?? m.User?.Email ?? "Unknown",
                    AvatarUrl = m.User?.UserProfile?.AvatarUrl,
                    Role = m.Role.ToString(),
                    JoinedAt = m.JoinedAt
                }).ToList() ?? new List<GroupMemberDto>()
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
