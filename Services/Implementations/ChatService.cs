using MentorEdu.DTOs.Chat;
using MentorEdu.DTOs.Common;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using MentorEdu.Services.Common;
using MentorEdu.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Services.Implementations
{
    public class ChatService : BaseService, IChatService
    {
        public ChatService(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
        }

        #region Conversations

        public async Task<ServiceResult<ConversationDetailDto>> GetConversationByIdAsync(long conversationId)
        {
            var conversation = await _unitOfWork.Conversations.GetByIdWithMessagesAsync(conversationId);
            
            if (conversation == null)
                return ServiceResult<ConversationDetailDto>.NotFoundResult("Conversation not found");

            var dto = MapToConversationDetailDto(conversation);
            return ServiceResult<ConversationDetailDto>.SuccessResult(dto);
        }

        public async Task<ServiceResult<List<ConversationListDto>>> GetUserConversationsAsync(long userId)
        {
            var conversations = await _unitOfWork.Conversations.GetByUserIdAsync(userId);

            var dtos = conversations.Select(c => MapToConversationListDto(c, userId)).ToList();

            // Sort by last message time
            dtos = dtos.OrderByDescending(d => d.LastMessageAt ?? d.CreatedAt).ToList();

            return ServiceResult<List<ConversationListDto>>.SuccessResult(dtos);
        }

        public async Task<ServiceResult<ConversationDetailDto>> GetOrCreateConversationAsync(long userId, CreateConversationDto request)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return ServiceResult<ConversationDetailDto>.NotFoundResult("User not found");

            var participant = await _unitOfWork.Users.GetByIdAsync(request.ParticipantId);
            if (participant == null)
                return ServiceResult<ConversationDetailDto>.NotFoundResult("Participant not found");

            // Determine student and mentor
            long studentId, mentorId;
            if (user.Role == UserRole.Student)
            {
                studentId = userId;
                mentorId = request.ParticipantId;
            }
            else
            {
                studentId = request.ParticipantId;
                mentorId = userId;
            }

            // Try to find existing conversation
            var existingConversation = await _unitOfWork.Conversations.Query()
                .Include(c => c.Student).ThenInclude(s => s.UserProfile)
                .Include(c => c.Mentor).ThenInclude(m => m.UserProfile)
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(50))
                .FirstOrDefaultAsync(c => 
                    c.StudentId == studentId && c.MentorId == mentorId &&
                    (request.RequestId == null || c.RequestId == request.RequestId) &&
                    (request.OrderId == null || c.OrderId == request.OrderId));

            if (existingConversation != null)
            {
                var dto = MapToConversationDetailDto(existingConversation);
                return ServiceResult<ConversationDetailDto>.SuccessResult(dto);
            }

            // Create new conversation
            var newConversation = new Conversation
            {
                RequestId = request.RequestId,
                OrderId = request.OrderId,
                StudentId = studentId,
                MentorId = mentorId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Conversations.AddAsync(newConversation);
            await _unitOfWork.SaveChangesAsync();

            // Reload with includes
            return await GetConversationByIdAsync(newConversation.Id);
        }

        public async Task<bool> UserHasAccessToConversationAsync(long userId, long conversationId)
        {
            var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
            if (conversation == null)
                return false;

            return conversation.StudentId == userId || conversation.MentorId == userId;
        }

        #endregion

        #region Messages

        public async Task<ServiceResult<MessageDto>> SendMessageAsync(long userId, SendMessageDto request)
        {
            var conversation = await _unitOfWork.Conversations.GetByIdAsync(request.ConversationId);
            
            if (conversation == null)
                return ServiceResult<MessageDto>.NotFoundResult("Conversation not found");

            if (conversation.StudentId != userId && conversation.MentorId != userId)
                return ServiceResult<MessageDto>.ForbiddenResult("You do not have permission to send messages");

            var user = await _unitOfWork.Users.GetByIdWithProfilesAsync(userId);

            var message = new Message
            {
                ConversationId = request.ConversationId,
                SenderId = userId,
                Content = request.Content,
                MessageType = request.MessageType,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Messages.AddAsync(message);
            await _unitOfWork.SaveChangesAsync();

            var dto = new MessageDto
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                SenderId = userId,
                SenderName = user?.UserProfile?.FullName,
                SenderAvatar = user?.UserProfile?.AvatarUrl,
                Content = message.Content,
                MessageType = message.MessageType,
                IsRead = message.IsRead,
                CreatedAt = message.CreatedAt,
                IsOwn = true
            };

            return ServiceResult<MessageDto>.SuccessResult(dto);
        }

        public async Task<ServiceResult<MessageDto>> SendMessageWithFilesAsync(long userId, SendMessageWithFileDto request)
        {
            var conversation = await _unitOfWork.Conversations.GetByIdAsync(request.ConversationId);
            
            if (conversation == null)
                return ServiceResult<MessageDto>.NotFoundResult("Conversation not found");

            if (conversation.StudentId != userId && conversation.MentorId != userId)
                return ServiceResult<MessageDto>.ForbiddenResult("You do not have permission to send messages");

            var user = await _unitOfWork.Users.GetByIdWithProfilesAsync(userId);

            return await _unitOfWork.ExecuteWithStrategyAsync(async () =>
            {
                var message = new Message
                {
                    ConversationId = request.ConversationId,
                    SenderId = userId,
                    Content = request.Content,
                    MessageType = request.Files?.Any() == true ? MessageType.File : MessageType.Text,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Messages.AddAsync(message);
                await _unitOfWork.SaveChangesAsync();

                // Add files
                var files = new List<MessageFileDto>();
                if (request.Files != null)
                {
                    foreach (var file in request.Files)
                    {
                        var messageFile = new MessageFile
                        {
                            MessageId = message.Id,
                            FileUrl = file.FileUrl,
                            FileType = file.FileType,
                            FileName = file.FileName,
                            FileSize = file.FileSize
                        };

                        await _unitOfWork.Messages.Query()
                            .Include(m => m.MessageFiles)
                            .FirstOrDefaultAsync(m => m.Id == message.Id);
                        
                        // Add to context manually since we don't have MessageFile repository
                        message.MessageFiles ??= new List<MessageFile>();
                        message.MessageFiles.Add(messageFile);

                        files.Add(new MessageFileDto
                        {
                            Id = messageFile.Id,
                            FileUrl = messageFile.FileUrl,
                            FileType = messageFile.FileType,
                            FileName = messageFile.FileName,
                            FileSize = messageFile.FileSize
                        });
                    }
                }

                await _unitOfWork.SaveChangesAsync();

                var dto = new MessageDto
                {
                    Id = message.Id,
                    ConversationId = message.ConversationId,
                    SenderId = userId,
                    SenderName = user?.UserProfile?.FullName,
                    SenderAvatar = user?.UserProfile?.AvatarUrl,
                    Content = message.Content,
                    MessageType = message.MessageType,
                    IsRead = message.IsRead,
                    CreatedAt = message.CreatedAt,
                    IsOwn = true,
                    Files = files
                };

                return ServiceResult<MessageDto>.SuccessResult(dto);
            });
        }

        public async Task<ServiceResult<PagedResponse<MessageDto>>> GetMessagesAsync(long userId, GetMessagesDto request)
        {
            var hasAccess = await UserHasAccessToConversationAsync(userId, request.ConversationId);
            if (!hasAccess)
                return ServiceResult<PagedResponse<MessageDto>>.ForbiddenResult("You do not have permission to view messages");

            var (messages, totalCount) = await _unitOfWork.Messages.GetPagedByConversationAsync(
                request.ConversationId, request.PageNumber, request.PageSize);

            var dtos = messages.Select(m => new MessageDto
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                SenderId = m.SenderId,
                SenderName = m.Sender?.UserProfile?.FullName,
                SenderAvatar = m.Sender?.UserProfile?.AvatarUrl,
                Content = m.Content,
                MessageType = m.MessageType,
                IsRead = m.IsRead,
                CreatedAt = m.CreatedAt,
                IsOwn = m.SenderId == userId,
                Files = m.MessageFiles?.Select(f => new MessageFileDto
                {
                    Id = f.Id,
                    FileUrl = f.FileUrl,
                    FileType = f.FileType,
                    FileName = f.FileName,
                    FileSize = f.FileSize
                }).ToList()
            });

            var response = PagedResponse<MessageDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount);
            return ServiceResult<PagedResponse<MessageDto>>.SuccessResult(response);
        }

        public async Task MarkMessagesAsReadAsync(long conversationId, long userId)
        {
            await _unitOfWork.Messages.MarkAsReadAsync(conversationId, userId);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<ServiceResult<int>> GetUnreadCountAsync(long userId)
        {
            var count = await _unitOfWork.Messages.GetUnreadCountByUserAsync(userId);
            return ServiceResult<int>.SuccessResult(count);
        }

        #endregion

        #region Private Methods

        private ConversationListDto MapToConversationListDto(Conversation conversation, long currentUserId)
        {
            var isStudent = conversation.StudentId == currentUserId;
            var participant = isStudent ? conversation.Mentor : conversation.Student;
            var lastMessage = conversation.Messages?.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
            var unreadCount = conversation.Messages?.Count(m => !m.IsRead && m.SenderId != currentUserId) ?? 0;

            return new ConversationListDto
            {
                Id = conversation.Id,
                RequestId = conversation.RequestId,
                RequestTitle = conversation.Request?.Title,
                OrderId = conversation.OrderId,
                CreatedAt = conversation.CreatedAt,
                ParticipantId = participant?.Id ?? 0,
                ParticipantName = participant?.UserProfile?.FullName,
                ParticipantAvatar = participant?.UserProfile?.AvatarUrl,
                IsParticipantMentor = !isStudent,
                LastMessage = lastMessage?.Content,
                LastMessageAt = lastMessage?.CreatedAt,
                LastMessageIsRead = lastMessage?.IsRead ?? true,
                UnreadCount = unreadCount
            };
        }

        private ConversationDetailDto MapToConversationDetailDto(Conversation conversation)
        {
            return new ConversationDetailDto
            {
                Id = conversation.Id,
                RequestId = conversation.RequestId,
                RequestTitle = conversation.Request?.Title,
                OrderId = conversation.OrderId,
                CreatedAt = conversation.CreatedAt,
                Student = new ParticipantDto
                {
                    Id = conversation.StudentId,
                    FullName = conversation.Student?.UserProfile?.FullName,
                    AvatarUrl = conversation.Student?.UserProfile?.AvatarUrl,
                    IsOnline = Hubs.ChatHub.IsUserOnline(conversation.StudentId)
                },
                Mentor = new ParticipantDto
                {
                    Id = conversation.MentorId,
                    FullName = conversation.Mentor?.UserProfile?.FullName,
                    AvatarUrl = conversation.Mentor?.UserProfile?.AvatarUrl,
                    IsOnline = Hubs.ChatHub.IsUserOnline(conversation.MentorId)
                },
                Messages = conversation.Messages?
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new MessageDto
                    {
                        Id = m.Id,
                        ConversationId = m.ConversationId,
                        SenderId = m.SenderId,
                        SenderName = m.Sender?.UserProfile?.FullName,
                        SenderAvatar = m.Sender?.UserProfile?.AvatarUrl,
                        Content = m.Content,
                        MessageType = m.MessageType,
                        IsRead = m.IsRead,
                        CreatedAt = m.CreatedAt,
                        Files = m.MessageFiles?.Select(f => new MessageFileDto
                        {
                            Id = f.Id,
                            FileUrl = f.FileUrl,
                            FileType = f.FileType,
                            FileName = f.FileName,
                            FileSize = f.FileSize
                        }).ToList()
                    }).ToList(),
                TotalMessages = conversation.Messages?.Count ?? 0
            };
        }

        #endregion
    }
}
