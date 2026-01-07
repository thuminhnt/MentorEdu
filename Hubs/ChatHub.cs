using System.Security.Claims;
using MentorEdu.DTOs.Chat;
using MentorEdu.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MentorEdu.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time chat between Mentor and Student
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatHub> _logger;

        // Track connected users: userId -> connectionId
        private static readonly Dictionary<long, HashSet<string>> _userConnections = new();
        private static readonly object _lock = new();

        public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        /// <summary>
        /// Called when a client connects
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            if (userId > 0)
            {
                lock (_lock)
                {
                    if (!_userConnections.ContainsKey(userId))
                        _userConnections[userId] = new HashSet<string>();
                    
                    _userConnections[userId].Add(Context.ConnectionId);
                }

                // Notify others that user is online
                await Clients.Others.SendAsync("UserOnline", userId);
                
                _logger.LogInformation("User {UserId} connected with connection {ConnectionId}", userId, Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetCurrentUserId();
            if (userId > 0)
            {
                lock (_lock)
                {
                    if (_userConnections.ContainsKey(userId))
                    {
                        _userConnections[userId].Remove(Context.ConnectionId);
                        
                        if (_userConnections[userId].Count == 0)
                        {
                            _userConnections.Remove(userId);
                            // Notify others that user is offline
                            Clients.Others.SendAsync("UserOffline", userId);
                        }
                    }
                }

                _logger.LogInformation("User {UserId} disconnected from connection {ConnectionId}", userId, Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Join a conversation room
        /// </summary>
        public async Task JoinConversation(long conversationId)
        {
            var userId = GetCurrentUserId();
            
            // Verify user has access to this conversation
            var hasAccess = await _chatService.UserHasAccessToConversationAsync(userId, conversationId);
            if (!hasAccess)
            {
                await Clients.Caller.SendAsync("Error", "You do not have access to this conversation");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
            
            // Mark messages as read when joining
            await _chatService.MarkMessagesAsReadAsync(conversationId, userId);
            
            _logger.LogInformation("User {UserId} joined conversation {ConversationId}", userId, conversationId);
        }

        /// <summary>
        /// Leave a conversation room
        /// </summary>
        public async Task LeaveConversation(long conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
            
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} left conversation {ConversationId}", userId, conversationId);
        }

        /// <summary>
        /// Send a message to a conversation
        /// </summary>
        public async Task SendMessage(SendMessageRequest request)
        {
            var userId = GetCurrentUserId();
            
            // Verify user has access
            var hasAccess = await _chatService.UserHasAccessToConversationAsync(userId, request.ConversationId);
            if (!hasAccess)
            {
                await Clients.Caller.SendAsync("Error", "You do not have permission to send messages in this conversation");
                return;
            }

            // Save message to database
            var result = await _chatService.SendMessageAsync(userId, new SendMessageDto
            {
                ConversationId = request.ConversationId,
                Content = request.Content,
                MessageType = request.MessageType
            });

            if (result.Success && result.Data != null)
            {
                // Broadcast to all users in the conversation
                await Clients.Group($"conversation_{request.ConversationId}")
                    .SendAsync("ReceiveMessage", result.Data);

                // Send notification to the other user if they're not in the conversation
                var conversation = await _chatService.GetConversationByIdAsync(request.ConversationId);
                if (conversation.Success && conversation.Data != null)
                {
                    var otherUserId = conversation.Data.Student?.Id == userId 
                        ? conversation.Data.Mentor?.Id 
                        : conversation.Data.Student?.Id;

                    if (otherUserId.HasValue)
                    {
                        await SendToUser(otherUserId.Value, "NewMessageNotification", new
                        {
                            ConversationId = request.ConversationId,
                            Message = result.Data,
                            SenderName = result.Data.SenderName
                        });
                    }
                }

                _logger.LogInformation("User {UserId} sent message to conversation {ConversationId}", userId, request.ConversationId);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", result.Message);
            }
        }

        /// <summary>
        /// Send a message with files
        /// </summary>
        public async Task SendMessageWithFiles(SendMessageWithFilesRequest request)
        {
            var userId = GetCurrentUserId();
            
            var hasAccess = await _chatService.UserHasAccessToConversationAsync(userId, request.ConversationId);
            if (!hasAccess)
            {
                await Clients.Caller.SendAsync("Error", "You do not have permission to send messages");
                return;
            }

            var result = await _chatService.SendMessageWithFilesAsync(userId, new SendMessageWithFileDto
            {
                ConversationId = request.ConversationId,
                Content = request.Content,
                Files = request.Files?.Select(f => new MessageFileUploadDto
                {
                    FileUrl = f.FileUrl,
                    FileType = f.FileType,
                    FileName = f.FileName,
                    FileSize = f.FileSize
                }).ToList()
            });

            if (result.Success && result.Data != null)
            {
                await Clients.Group($"conversation_{request.ConversationId}")
                    .SendAsync("ReceiveMessage", result.Data);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", result.Message);
            }
        }

        /// <summary>
        /// Notify typing status
        /// </summary>
        public async Task Typing(long conversationId, bool isTyping)
        {
            var userId = GetCurrentUserId();
            
            await Clients.OthersInGroup($"conversation_{conversationId}")
                .SendAsync("UserTyping", new { UserId = userId, ConversationId = conversationId, IsTyping = isTyping });
        }

        /// <summary>
        /// Mark messages as read
        /// </summary>
        public async Task MarkAsRead(long conversationId)
        {
            var userId = GetCurrentUserId();
            
            await _chatService.MarkMessagesAsReadAsync(conversationId, userId);
            
            // Notify the other user that messages have been read
            await Clients.OthersInGroup($"conversation_{conversationId}")
                .SendAsync("MessagesRead", new { ConversationId = conversationId, ReadByUserId = userId });
        }

        /// <summary>
        /// Get online status of users
        /// </summary>
        public async Task GetOnlineUsers(List<long> userIds)
        {
            var onlineUsers = new List<long>();
            
            lock (_lock)
            {
                foreach (var userId in userIds)
                {
                    if (_userConnections.ContainsKey(userId) && _userConnections[userId].Count > 0)
                    {
                        onlineUsers.Add(userId);
                    }
                }
            }

            await Clients.Caller.SendAsync("OnlineUsers", onlineUsers);
        }

        #region Private Methods

        private long GetCurrentUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private async Task SendToUser(long userId, string method, object data)
        {
            HashSet<string>? connections;
            
            lock (_lock)
            {
                _userConnections.TryGetValue(userId, out connections);
            }

            if (connections != null && connections.Count > 0)
            {
                await Clients.Clients(connections.ToList()).SendAsync(method, data);
            }
        }

        /// <summary>
        /// Check if a user is online
        /// </summary>
        public static bool IsUserOnline(long userId)
        {
            lock (_lock)
            {
                return _userConnections.ContainsKey(userId) && _userConnections[userId].Count > 0;
            }
        }

        #endregion
    }

    #region Request DTOs for Hub

    public class SendMessageRequest
    {
        public long ConversationId { get; set; }
        public string Content { get; set; } = string.Empty;
        public MentorEdu.Models.Enums.MessageType MessageType { get; set; } = MentorEdu.Models.Enums.MessageType.Text;
    }

    public class SendMessageWithFilesRequest
    {
        public long ConversationId { get; set; }
        public string? Content { get; set; }
        public List<FileUploadRequest>? Files { get; set; }
    }

    public class FileUploadRequest
    {
        public string FileUrl { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }

    #endregion
}
