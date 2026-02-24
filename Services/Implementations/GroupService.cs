using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Groups;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
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
                Console.WriteLine($"[GroupService] Creating group for mentorId: {mentorId}, Name: {dto.Name}");
                
                // Verify mentor exists
                var mentor = await _userRepository.GetByIdWithProfilesAsync(mentorId);
                if (mentor == null)
                {
                    Console.WriteLine($"[GroupService] ERROR: Mentor not found with ID: {mentorId}");
                    return ServiceResult<GroupDto>.Failure("Mentor not found");
                }
                
              

                // Generate unique invite code
                var inviteCode = await GenerateUniqueInviteCodeAsync();
                Console.WriteLine($"[GroupService] Generated invite code: {inviteCode}");

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
                Console.WriteLine($"[GroupService] Saving group to database...");
                await _unitOfWork.SaveChangesAsync();
                Console.WriteLine($"[GroupService] Group saved successfully with ID: {group.Id}");

                // Manually attach properties to avoid reloading from DB
                // This ensures we have the necessary navigation properties for mapping
                group.Mentor = mentor;
                // Note: mentorMember.User setter might be needed if not virtual, but assuming EF Core POCO
                mentorMember.User = mentor;
                group.Members = new List<GroupMember> { mentorMember };

                Console.WriteLine($"[GroupService] Mapping group to DTO...");
                GroupDto groupDto;
                try
                {
                    groupDto = await MapToGroupDtoAsync(group, mentorId);
                    Console.WriteLine($"[GroupService] Mapping successful");
                }
                catch (Exception mapEx)
                {
                    Console.WriteLine($"[GroupService] MAPPING ERROR: {mapEx.Message}");
                    Console.WriteLine($"[GroupService] StackTrace: {mapEx.StackTrace}");
                    throw; // Re-throw to outer catch
                }
                
                Console.WriteLine($"[GroupService] Group created successfully: ID={groupDto.Id}, Name={groupDto.Name}");
                return ServiceResult<GroupDto>.SuccessResult(groupDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GroupService] EXCEPTION: {ex.GetType().Name}");
                Console.WriteLine($"[GroupService] Message: {ex.Message}");
                Console.WriteLine($"[GroupService] StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[GroupService] InnerException: {ex.InnerException.Message}");
                }
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

                // Get user info for system message
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
                
                // Create system message for member joined
                var systemMessage = new GroupMessage
                {
                    GroupId = group.Id,
                    SenderId = userId,
                    Content = $"{userName} đã tham gia nhóm",
                    MessageType = MessageType.System,
                    CreatedAt = DateTime.UtcNow
                };
                await _groupMessageRepository.AddAsync(systemMessage);
                
                await _unitOfWork.SaveChangesAsync();

                // Reload group with updated members
                var updatedGroup = await _groupRepository.GetByIdAsync(group.Id);
                if (updatedGroup == null)
                {
                    return ServiceResult<GroupDto>.Failure("Failed to retrieve group");
                }

                var groupDto = await MapToGroupDtoAsync(updatedGroup, userId);
                return ServiceResult<GroupDto>.SuccessResult(groupDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<GroupDto>.Failure($"Error joining group: {ex.Message}");
            }
        }

        public async Task<ServiceResult<GroupDto>> AddMemberAsync(long adminId, long groupId, long userId)
        {
            try
            {
                var group = await _groupRepository.GetByIdAsync(groupId);
                if (group == null)
                {
                    return ServiceResult<GroupDto>.Failure("Group not found");
                }

                // Check if admin has permission
                var adminMember = await _groupMemberRepository.GetMemberAsync(groupId, adminId);
                if (adminMember == null || adminMember.Role != GroupRole.Admin)
                {
                    return ServiceResult<GroupDto>.Failure("Only group admin can add members");
                }

                // Check if user is already a member
                var existingMember = await _groupMemberRepository.GetMemberAsync(groupId, userId);
                if (existingMember != null)
                {
                    return ServiceResult<GroupDto>.Failure("User is already a member of this group");
                }

                // Get user info for system message
                var user = await _userRepository.GetByIdWithProfilesAsync(userId);
                if (user == null)
                {
                    return ServiceResult<GroupDto>.Failure("User not found");
                }
                var userName = user.UserProfile?.FullName ?? user.Email ?? "Someone";

                // Add new member
                var member = new GroupMember
                {
                    GroupId = groupId,
                    UserId = userId,
                    Role = GroupRole.Member,
                    JoinedAt = DateTime.UtcNow
                };
                await _groupMemberRepository.AddAsync(member);

                // Create system message for member added
                var systemMessage = new GroupMessage
                {
                    GroupId = groupId,
                    SenderId = adminId,
                    Content = $"{userName} đã được thêm vào nhóm",
                    MessageType = MessageType.System,
                    CreatedAt = DateTime.UtcNow
                };
                await _groupMessageRepository.AddAsync(systemMessage);

                await _unitOfWork.SaveChangesAsync();

                // Reload group with updated members
                var updatedGroup = await _groupRepository.GetByIdAsync(groupId);
                if (updatedGroup == null)
                {
                    return ServiceResult<GroupDto>.Failure("Failed to retrieve group");
                }

                var groupDto = await MapToGroupDtoAsync(updatedGroup, adminId);
                return ServiceResult<GroupDto>.SuccessResult(groupDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<GroupDto>.Failure($"Error adding member: {ex.Message}");
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

                var groupDto = await MapToGroupDtoAsync(group);
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
                var groupDtos = new List<GroupDto>();
                foreach (var g in groups)
                {
                    groupDtos.Add(await MapToGroupDtoAsync(g, userId));
                }
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

                // Reload to get navigation properties (sender info)
                var savedMessage = await _groupMessageRepository.GetByIdWithSenderAsync(message.Id);
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

        public async Task<ServiceResult<object>> GetGroupMessagesAsync(long groupId, long userId, int pageNumber = 1, int pageSize = 50)
        {
            try
            {
                // Verify user has access
                var isMember = await _groupMemberRepository.IsMemberAsync(groupId, userId);
                if (!isMember)
                {
                    return ServiceResult<object>.Failure("You do not have access to this group");
                }

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
            catch (Exception ex)
            {
                return ServiceResult<object>.Failure($"Error retrieving messages: {ex.Message}");
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

                // Get user name for system message
                var userName = member.User?.UserProfile?.FullName ?? member.User?.Email ?? "Unknown";

                // Create system message before removing member
                var systemMessage = new GroupMessage
                {
                    GroupId = groupId,
                    SenderId = userId,
                    Content = $"{userName} đã rời nhóm",
                    MessageType = MessageType.System,
                    CreatedAt = DateTime.UtcNow
                };
                await _groupMessageRepository.AddAsync(systemMessage);

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

        private string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private async Task<GroupDto> MapToGroupDtoAsync(Group group, long? userId = null)
        {
            Console.WriteLine($"[MapToGroupDto] Starting - GroupId: {group?.Id}, Name: {group?.Name}");
            Console.WriteLine($"[MapToGroupDto] Mentor: {(group?.Mentor != null ? "not null" : "NULL")}");
            Console.WriteLine($"[MapToGroupDto] Mentor.UserProfile: {(group?.Mentor?.UserProfile != null ? "not null" : "NULL")}");
            Console.WriteLine($"[MapToGroupDto] Members count: {group?.Members?.Count ?? 0}");
            
            // Get latest message for preview
            var latestMessage = await _groupMessageRepository.GetLatestMessageByGroupIdAsync(group.Id);
            
            // Format lastMessage based on messageType
            string displayMessage = latestMessage?.Content;
            if (latestMessage != null)
            {
                if (latestMessage.MessageType == MentorEdu.Models.Enums.MessageType.Image)
                {
                    displayMessage = "📷 Ảnh";
                }
                else if (latestMessage.MessageType == MentorEdu.Models.Enums.MessageType.File)
                {
                    displayMessage = "📄 File";
                }
            }

            // Compute unread count if userId is provided
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
