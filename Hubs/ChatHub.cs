using System.Collections.Concurrent;
using System.Security.Claims;
using MentorEdu.DTOs.Chat;
using MentorEdu.Models.Enums;
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
        private readonly IGroupService _groupService;
        private readonly IWorkActionService _workActionService;
        private readonly ILogger<ChatHub> _logger;

        // Track connected users: userId -> connectionIds
        private static readonly ConcurrentDictionary<long, HashSet<string>> _userConnections = new();

        public ChatHub(
            IChatService chatService,
            IGroupService groupService,
            IWorkActionService workActionService,
            ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _groupService = groupService;
            _workActionService = workActionService;
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
                var conns = _userConnections.GetOrAdd(userId, _ => new HashSet<string>());
                lock (conns) conns.Add(Context.ConnectionId);

                // Add user to personal group for real-time notifications
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

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
                if (_userConnections.TryGetValue(userId, out var conns))
                {
                    bool isEmpty;
                    lock (conns)
                    {
                        conns.Remove(Context.ConnectionId);
                        isEmpty = conns.Count == 0;
                    }
                    if (isEmpty)
                    {
                        _userConnections.TryRemove(userId, out _);
                        await Clients.Others.SendAsync("UserOffline", userId);
                    }
                }

                _logger.LogInformation("User {UserId} disconnected from connection {ConnectionId}", userId, Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        #region Community Post Rooms

        /// <summary>
        /// Join a community post room to receive real-time comments
        /// </summary>
        public async Task JoinPostRoom(long postId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"post_{postId}");
            _logger.LogInformation("User {UserId} joined post room {PostId}", GetCurrentUserId(), postId);
        }

        /// <summary>
        /// Leave a community post room
        /// </summary>
        public async Task LeavePostRoom(long postId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"post_{postId}");
            _logger.LogInformation("User {UserId} left post room {PostId}", GetCurrentUserId(), postId);
        }

        #endregion

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
            
            // Check for any pending work actions targeting this user in this conversation
            var pendingForUser = _workActionService.GetPendingActionsForUser(userId, conversationId).ToList();

            foreach (var pending in pendingForUser)
            {
                await Clients.Caller.SendAsync("WorkActionPopup", new
                {
                    requestId = pending.RequestId,
                    actionType = pending.ActionType.ToString(),
                    conversationId = pending.ConversationId,
                    orderId = pending.OrderId,
                    requestedByUserId = pending.RequestedByUserId
                });
            }
            
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
            
            foreach (var userId in userIds)
            {
                if (_userConnections.TryGetValue(userId, out var c) && c.Count > 0)
                    onlineUsers.Add(userId);
            }

            await Clients.Caller.SendAsync("OnlineUsers", onlineUsers);
        }

        #region Group Chat Methods

        /// <summary>
        /// Join a group chat room
        /// </summary>
        public async Task JoinGroupRoom(long groupId)
        {
            var userId = GetCurrentUserId();

            // Verify user has access to this group
            var hasAccess = await _groupService.UserHasAccessToGroupAsync(userId, groupId);
            if (!hasAccess.Success || !hasAccess.Data)
            {
                await Clients.Caller.SendAsync("Error", "You do not have access to this group");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{groupId}");

            _logger.LogInformation("User {UserId} joined group {GroupId}", userId, groupId);
        }

        /// <summary>
        /// Leave a group chat room
        /// </summary>
        public async Task LeaveGroupRoom(long groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"group_{groupId}");

            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} left group {GroupId}", userId, groupId);
        }

        /// <summary>
        /// Send a message to a group
        /// </summary>
        public async Task SendGroupMessage(SendGroupMessageRequest request)
        {
            var userId = GetCurrentUserId();

            // Verify user has access
            var hasAccess = await _groupService.UserHasAccessToGroupAsync(userId, request.GroupId);
            if (!hasAccess.Success || !hasAccess.Data)
            {
                await Clients.Caller.SendAsync("Error", "You do not have permission to send messages in this group");
                return;
            }

            // Save message to database
            var result = await _groupService.SendGroupMessageAsync(userId, new DTOs.Groups.SendGroupMessageDto
            {
                GroupId = request.GroupId,
                Content = request.Content,
                MessageType = request.MessageType
            });

            if (result.Success && result.Data != null)
            {
                // Broadcast to all users in the group
                await Clients.Group($"group_{request.GroupId}")
                    .SendAsync("ReceiveGroupMessage", result.Data);

                _logger.LogInformation("User {UserId} sent message to group {GroupId}", userId, request.GroupId);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", result.Message);
            }
        }

        /// <summary>
        /// Notify typing status in group
        /// </summary>
        public async Task GroupTyping(long groupId, bool isTyping)
        {
            var userId = GetCurrentUserId();

            await Clients.OthersInGroup($"group_{groupId}")
                .SendAsync("UserGroupTyping", new { UserId = userId, GroupId = groupId, IsTyping = isTyping });
        }

        /// <summary>
        /// Mark all group messages as read for the current user
        /// </summary>
        public async Task MarkGroupAsRead(long groupId)
        {
            var userId = GetCurrentUserId();

            await _groupService.MarkGroupAsReadAsync(groupId, userId);

            // Notify the caller that messages are read
            await Clients.Caller.SendAsync("GroupMessagesRead", new { GroupId = groupId });

            _logger.LogInformation("User {UserId} marked group {GroupId} as read", userId, groupId);
        }

        #endregion

        /// <summary>Request to start a work session</summary>
        public async Task RequestStartWork(long conversationId, long orderId, long mentorId, long studentId)
        {
            var userId = GetCurrentUserId();
            var result = await _workActionService.RequestStartWorkAsync(userId, conversationId, orderId, mentorId, studentId);

            if (!result.Success)
            {
                await Clients.Caller.SendAsync("Error", result.Error);
                return;
            }

            await Clients.Group($"conversation_{conversationId}").SendAsync("WorkActionState", new
            {
                requestId = result.RequestId,
                actionType = result.ActionType.ToString(),
                status = "Waiting",
                conversationId,
                orderId
            });

            await SendToUser(result.TargetUserId, "WorkActionPopup", new
            {
                requestId = result.RequestId,
                actionType = result.ActionType.ToString(),
                conversationId,
                orderId,
                mentorId,
                studentId,
                requestedByUserId = userId
            });

            await Clients.Caller.SendAsync("WorkActionSent", new
            {
                requestId = result.RequestId,
                actionType = result.ActionType.ToString()
            });
        }

        /// <summary>Request to pause a work session</summary>
        public async Task RequestPauseWork(long conversationId, long sessionId)
        {
            var userId = GetCurrentUserId();
            var result = await _workActionService.RequestPauseWorkAsync(userId, conversationId, sessionId);
            await BroadcastWorkActionPending(result, conversationId, sessionId: sessionId);
        }

        /// <summary>Request to end a work session</summary>
        public async Task RequestEndWork(long conversationId, long sessionId)
        {
            var userId = GetCurrentUserId();
            var result = await _workActionService.RequestEndWorkAsync(userId, conversationId, sessionId);
            await BroadcastWorkActionPending(result, conversationId, sessionId: sessionId);
        }

        /// <summary>Request to complete the order</summary>
        public async Task RequestCompleteOrder(long conversationId, long orderId)
        {
            var userId = GetCurrentUserId();
            var result = await _workActionService.RequestCompleteOrderAsync(userId, conversationId, orderId);

            if (!result.Success)
            {
                await Clients.Caller.SendAsync("Error", result.Error);
                return;
            }

            // Broadcast the system message that the service already persisted
            if (result.SystemMessageDto != null)
                await Clients.Group($"conversation_{conversationId}").SendAsync("ReceiveMessage", result.SystemMessageDto);

            await Clients.Group($"conversation_{conversationId}").SendAsync("WorkActionState", new
            {
                requestId = result.RequestId,
                actionType = result.ActionType.ToString(),
                status = "Waiting",
                conversationId,
                orderId
            });

            await SendToUser(result.TargetUserId, "WorkActionPopup", new
            {
                requestId = result.RequestId,
                actionType = result.ActionType.ToString(),
                conversationId,
                orderId,
                requestedByUserId = userId
            });

            await Clients.Caller.SendAsync("WorkActionSent", new
            {
                requestId = result.RequestId,
                actionType = result.ActionType.ToString()
            });
        }

        /// <summary>Respond to a pending work action (accept or reject)</summary>
        public async Task RespondWorkAction(string requestId, bool accept)
        {
            var userId = GetCurrentUserId();
            var result = await _workActionService.RespondWorkActionAsync(requestId, userId, accept);

            if (!result.Success)
            {
                await Clients.Caller.SendAsync("Error", result.Error);
                return;
            }

            if (!result.Accepted)
            {
                await SendToUser(result.RequestedByUserId, "WorkActionRejected", new
                {
                    requestId,
                    actionType = result.ActionType.ToString(),
                    conversationId = result.ConversationId
                });
                await Clients.Group($"conversation_{result.ConversationId}").SendAsync("WorkActionState", new
                {
                    requestId,
                    actionType = result.ActionType.ToString(),
                    status = "Rejected",
                    conversationId = result.ConversationId
                });
                return;
            }

            // Broadcast the appropriate event based on action type
            switch (result.ActionType)
            {
                case WorkActionTypeDto.Start:
                    await Clients.Group($"conversation_{result.ConversationId}").SendAsync("WorkSessionStarted", new
                    {
                        sessionId = result.SessionId,
                        orderId = result.OrderId,
                        startTime = result.StartTime
                    });
                    break;

                case WorkActionTypeDto.Pause:
                    await Clients.Group($"conversation_{result.ConversationId}").SendAsync("WorkSessionPaused", new
                    {
                        sessionId = result.SessionId,
                        orderId = result.OrderId,
                        totalMinutes = result.TotalMinutes
                    });
                    break;

                case WorkActionTypeDto.End:
                    await Clients.Group($"conversation_{result.ConversationId}").SendAsync("WorkSessionEnded", new
                    {
                        sessionId = result.SessionId,
                        orderId = result.OrderId,
                        totalMinutes = result.TotalMinutes,
                        endTime = result.EndTime
                    });
                    break;

                case WorkActionTypeDto.Complete:
                    if (result.SystemMessageDto != null)
                        await Clients.Group($"conversation_{result.ConversationId}").SendAsync("ReceiveMessage", result.SystemMessageDto);

                    await Clients.Group($"conversation_{result.ConversationId}").SendAsync("OrderCompleted", new
                    {
                        conversationId = result.ConversationId,
                        orderId = result.OrderId,
                        completedBy = result.CompletedBy,
                        completedAt = DateTime.UtcNow
                    });
                    break;
            }
        }
        #region Private Methods

        private long GetCurrentUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private async Task SendToUser(long userId, string method, object data)
        {
            if (_userConnections.TryGetValue(userId, out var connections) && connections.Count > 0)
                await Clients.Clients(connections.ToList()).SendAsync(method, data);
        }

        /// <summary>
        /// Shared helper: broadcast pending work action state after Pause/End requests.
        /// </summary>
        private async Task BroadcastWorkActionPending(WorkActionPendingResult result, long conversationId, long? sessionId = null)
        {
            if (!result.Success)
            {
                await Clients.Caller.SendAsync("Error", result.Error);
                return;
            }

            await Clients.Group($"conversation_{conversationId}").SendAsync("WorkActionState", new
            {
                requestId = result.RequestId,
                actionType = result.ActionType.ToString(),
                status = "Waiting",
                conversationId,
                sessionId
            });

            await SendToUser(result.TargetUserId, "WorkActionPopup", new
            {
                requestId = result.RequestId,
                actionType = result.ActionType.ToString(),
                conversationId,
                sessionId,
                requestedByUserId = result.RequestedByUserId
            });

            await Clients.Caller.SendAsync("WorkActionSent", new
            {
                requestId = result.RequestId,
                actionType = result.ActionType.ToString()
            });
        }
        /// <summary>
        /// Check if a user is online
        /// </summary>
        public static bool IsUserOnline(long userId) =>
            _userConnections.TryGetValue(userId, out var c) && c.Count > 0;

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

    public class SendGroupMessageRequest
    {
        public long GroupId { get; set; }
        public string Content { get; set; } = string.Empty;
        public MentorEdu.Models.Enums.MessageType MessageType { get; set; } = MentorEdu.Models.Enums.MessageType.Text;
    }


    #endregion
}