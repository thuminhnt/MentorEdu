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
        private readonly IWorkSessionService _workSessionService;
        private readonly IGroupService _groupService;
        private readonly ILogger<ChatHub> _logger;
        private readonly INotificationService _notificationService;

        private readonly IOrderService _orderService;

        // Track connected users: userId -> connectionId
        private static readonly Dictionary<long, HashSet<string>> _userConnections = new();
        private static readonly object _lock = new();
        private static readonly Dictionary<string, PendingWorkAction> _pendingWorkActions = new();
        private static readonly object _pendingWorkActionsLock = new();
        public ChatHub(IChatService chatService, IWorkSessionService workSessionService, IGroupService groupService, IOrderService orderService, INotificationService notificationService, ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _logger = logger;
            _workSessionService = workSessionService;
            _groupService = groupService;
            _orderService = orderService;
            _notificationService = notificationService;
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
            List<PendingWorkAction> pendingForUser;
            lock (_pendingWorkActionsLock)
            {
                pendingForUser = _pendingWorkActions.Values
                    .Where(a => a.TargetUserId == userId && a.ConversationId == conversationId)
                    .ToList();
            }

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

        /// <summary>
        /// Request to start a work session
        /// </summary>
        public async Task RequestStartWork(long conversationId, long orderId, long mentorId, long studentId)
        {
            var userId = GetCurrentUserId();
            if (!await _chatService.UserHasAccessToConversationAsync(userId, conversationId))
            {
                await Clients.Caller.SendAsync("Error", "You do not have access to this conversation");
                return;
            }

            var conversation = await _chatService.GetConversationByIdAsync(conversationId);
            if (!conversation.Success || conversation.Data == null)
            {
                await Clients.Caller.SendAsync("Error", "Conversation not found");
                return;
            }

            if (conversation.Data.Mentor?.Id != mentorId || conversation.Data.Student?.Id != studentId)
            {
                await Clients.Caller.SendAsync("Error", "Conversation participants do not match request");
                return;
            }

            if (userId != mentorId && userId != studentId)
            {
                await Clients.Caller.SendAsync("Error", "Invalid requester");
                return;
            }

            var targetUserId = userId == mentorId ? studentId : mentorId;
            var requestId = Guid.NewGuid().ToString("N");
            var pendingAction = new PendingWorkAction
            {
                RequestId = requestId,
                ActionType = WorkActionType.Start,
                ConversationId = conversationId,
                OrderId = orderId,
                SessionId = null,
                RequestedByUserId = userId,
                TargetUserId = targetUserId,
                MentorId = mentorId,
                StudentId = studentId,
                CreatedAtUtc = DateTime.UtcNow
            };

            lock (_pendingWorkActionsLock)
            {
                _pendingWorkActions[requestId] = pendingAction;
            }

            await Clients.Group($"conversation_{conversationId}")
                .SendAsync("WorkActionState", new
                {
                    requestId,
                    actionType = pendingAction.ActionType.ToString(),
                    status = "Waiting",
                    conversationId,
                    orderId
                });

            await SendToUser(targetUserId, "WorkActionPopup", new
            {
                requestId,
                actionType = pendingAction.ActionType.ToString(),
                conversationId,
                orderId,
                mentorId,
                studentId,
                requestedByUserId = userId
            });

            await Clients.Caller.SendAsync("WorkActionSent", new
            {
                requestId,
                actionType = pendingAction.ActionType.ToString()
            });
        }

        /// <summary>
        /// Request to pause a work session
        /// </summary>
        public async Task RequestPauseWork(long conversationId, long sessionId)
        {
            await RequestWorkAction(conversationId, sessionId, WorkActionType.Pause);
        }

        /// <summary>
        /// Request to end a work session
        /// </summary>
        public async Task RequestEndWork(long conversationId, long sessionId)
        {
            await RequestWorkAction(conversationId, sessionId, WorkActionType.End);
        }

        /// <summary>
        /// Request to complete the order
        /// </summary>
        public async Task RequestCompleteOrder(long conversationId, long orderId)
        {
            var userId = GetCurrentUserId();
            if (!await _chatService.UserHasAccessToConversationAsync(userId, conversationId))
            {
                await Clients.Caller.SendAsync("Error", "You do not have access to this conversation");
                return;
            }

            var conversation = await _chatService.GetConversationByIdAsync(conversationId);
            if (!conversation.Success || conversation.Data == null)
            {
                await Clients.Caller.SendAsync("Error", "Conversation not found");
                return;
            }

            if (conversation.Data.Mentor?.Id != userId && conversation.Data.Student?.Id != userId)
            {
                await Clients.Caller.SendAsync("Error", "Invalid requester");
                return;
            }

            // Find the other user and requester name
            var isMentor = conversation.Data.Mentor?.Id == userId;
            var otherUserId = isMentor ? conversation.Data.Student?.Id : conversation.Data.Mentor?.Id;
            var requesterName = isMentor
                ? (conversation.Data.Mentor?.FullName ?? "Mentor")
                : (conversation.Data.Student?.FullName ?? "Student");
            if (otherUserId == null)
            {
                 await Clients.Caller.SendAsync("Error", "Partner not found");
                 return;
            }

            // Prevent duplicate pending complete actions for the same conversation
            lock (_pendingWorkActionsLock)
            {
                var existingAction = _pendingWorkActions.Values
                    .FirstOrDefault(a => a.ConversationId == conversationId 
                        && a.ActionType == WorkActionType.Complete);
                if (existingAction != null)
                {
                    // Already has a pending complete request — don't create another
                    Clients.Caller.SendAsync("Error", "A completion request is already pending").Wait();
                    return;
                }
            }

            var requestId = Guid.NewGuid().ToString("N");
            var pendingAction = new PendingWorkAction
            {
                RequestId = requestId,
                ActionType = WorkActionType.Complete,
                ConversationId = conversationId,
                OrderId = orderId,
                SessionId = null,
                RequestedByUserId = userId,
                TargetUserId = otherUserId.Value,
                MentorId = conversation.Data.Mentor?.Id,
                StudentId = conversation.Data.Student?.Id,
                CreatedAtUtc = DateTime.UtcNow
            };

            lock (_pendingWorkActionsLock)
            {
                _pendingWorkActions[requestId] = pendingAction;
            }

            // Save a system message in the conversation so both users can see it in chat history
            var systemContent = $"{requesterName} has requested to complete the order. Waiting for confirmation...";
            var systemMsg = await _chatService.SendMessageAsync(userId, new SendMessageDto
            {
                ConversationId = conversationId,
                Content = systemContent,
                MessageType = MessageType.System
            });

            // Broadcast the system message to the conversation
            if (systemMsg.Success && systemMsg.Data != null)
            {
                await Clients.Group($"conversation_{conversationId}")
                    .SendAsync("ReceiveMessage", systemMsg.Data);
            }

            // Create a notification for the other user (works even if they're offline)
            await _notificationService.CreateNotificationAsync(
                otherUserId.Value,
                NotificationType.OrderCompleted,
                "Order Completion Request",
                $"{requesterName} has requested to complete the order. Please open chat to respond.",
                $"{{\"conversationId\":{conversationId},\"orderId\":{orderId}}}"
            );

            await Clients.Group($"conversation_{conversationId}")
                .SendAsync("WorkActionState", new
                {
                    requestId,
                    actionType = pendingAction.ActionType.ToString(),
                    status = "Waiting",
                    conversationId,
                    orderId
                });

            await SendToUser(otherUserId.Value, "WorkActionPopup", new
            {
                requestId,
                actionType = pendingAction.ActionType.ToString(),
                conversationId,
                orderId,
                requestedByUserId = userId
            });

            await Clients.Caller.SendAsync("WorkActionSent", new
            {
                requestId,
                actionType = pendingAction.ActionType.ToString()
            });
        }

        /// <summary>
        /// Respond to a pending work action
        /// </summary>
        public async Task RespondWorkAction(string requestId, bool accept)
        {
            var userId = GetCurrentUserId();
            PendingWorkAction? pendingAction;

            lock (_pendingWorkActionsLock)
            {
                if (!_pendingWorkActions.TryGetValue(requestId, out pendingAction))
                {
                    pendingAction = null;
                }
                else
                {
                    _pendingWorkActions.Remove(requestId);
                }
            }

            if (pendingAction == null)
            {
                await Clients.Caller.SendAsync("Error", "Request not found or already handled");
                return;
            }

            if (pendingAction.TargetUserId != userId)
            {
                await Clients.Caller.SendAsync("Error", "You are not allowed to respond to this request");
                return;
            }

            if (!accept)
            {
                await SendToUser(pendingAction.RequestedByUserId, "WorkActionRejected", new
                {
                    requestId,
                    actionType = pendingAction.ActionType.ToString(),
                    conversationId = pendingAction.ConversationId
                });

                await Clients.Group($"conversation_{pendingAction.ConversationId}")
                    .SendAsync("WorkActionState", new
                    {
                        requestId,
                        actionType = pendingAction.ActionType.ToString(),
                        status = "Rejected",
                        conversationId = pendingAction.ConversationId
                    });

                return;
            }

            switch (pendingAction.ActionType)
            {
                case WorkActionType.Start:
                    {
                        if (!pendingAction.OrderId.HasValue || pendingAction.MentorId == null || pendingAction.StudentId == null)
                        {
                            await Clients.Caller.SendAsync("Error", "Missing session details");
                            return;
                        }

                        var startResult = await _workSessionService.StartSessionAsync(
                            pendingAction.OrderId.Value,
                            pendingAction.MentorId.Value,
                            pendingAction.StudentId.Value);

                        if (!startResult.Success || startResult.Data == null)
                        {
                            await SendToUser(pendingAction.RequestedByUserId, "Error", startResult.Message ?? "Failed to start session");
                            await Clients.Caller.SendAsync("Error", startResult.Message ?? "Failed to start session");
                            return;
                        }

                        await Clients.Group($"conversation_{pendingAction.ConversationId}")
                            .SendAsync("WorkSessionStarted", new
                            {
                                sessionId = startResult.Data.SessionId,
                                orderId = pendingAction.OrderId.Value,
                                startTime = startResult.Data.StartTime
                            });
                        break;
                    }
                case WorkActionType.Pause:
                    {
                        if (!pendingAction.SessionId.HasValue)
                        {
                            await Clients.Caller.SendAsync("Error", "Missing session id");
                            return;
                        }

                        var pauseResult = await _workSessionService.PauseSessionAsync(pendingAction.SessionId.Value);
                        if (!pauseResult.Success || pauseResult.Data == null)
                        {
                            await SendToUser(pendingAction.RequestedByUserId, "Error", pauseResult.Message ?? "Failed to pause session");
                            await Clients.Caller.SendAsync("Error", pauseResult.Message ?? "Failed to pause session");
                            return;
                        }

                        await Clients.Group($"conversation_{pendingAction.ConversationId}")
                            .SendAsync("WorkSessionPaused", new
                            {
                                sessionId = pauseResult.Data.Id,
                                orderId = pauseResult.Data.OrderId,
                                totalMinutes = pauseResult.Data.TotalMinutes
                            });
                        break;
                    }
                case WorkActionType.End:
                    {
                        if (!pendingAction.SessionId.HasValue)
                        {
                            await Clients.Caller.SendAsync("Error", "Missing session id");
                            return;
                        }

                        var endResult = await _workSessionService.EndSessionAsync(pendingAction.SessionId.Value);
                        if (!endResult.Success || endResult.Data == null)
                        {
                            await SendToUser(pendingAction.RequestedByUserId, "Error", endResult.Message ?? "Failed to end session");
                            await Clients.Caller.SendAsync("Error", endResult.Message ?? "Failed to end session");
                            return;
                        }

                        await Clients.Group($"conversation_{pendingAction.ConversationId}")
                            .SendAsync("WorkSessionEnded", new
                            {
                                sessionId = endResult.Data.Id,
                                orderId = endResult.Data.OrderId,
                                totalMinutes = endResult.Data.TotalMinutes,
                                endTime = endResult.Data.EndTime
                            });
                        break;
                    }
                case WorkActionType.Complete:
                    {
                        if (!pendingAction.OrderId.HasValue)
                        {
                            await Clients.Caller.SendAsync("Error", "Missing order id");
                            return;
                        }

                        // Call OrderService to complete
                        var completeResult = await _orderService.CompleteOrderAsync(pendingAction.OrderId.Value, userId);
                        
                        if (!completeResult.Success)
                        {
                             await SendToUser(pendingAction.RequestedByUserId, "Error", completeResult.Message ?? "Failed to complete order");
                             await Clients.Caller.SendAsync("Error", completeResult.Message ?? "Failed to complete order");
                             return;
                        }

                        // Send a system message confirming order completion
                        var completeSystemMsg = await _chatService.SendMessageAsync(userId, new SendMessageDto
                        {
                            ConversationId = pendingAction.ConversationId,
                            Content = " Order has been completed successfully! Thank you for using EduMentor.",
                            MessageType = MessageType.System
                        });

                        if (completeSystemMsg.Success && completeSystemMsg.Data != null)
                        {
                            await Clients.Group($"conversation_{pendingAction.ConversationId}")
                                .SendAsync("ReceiveMessage", completeSystemMsg.Data);
                        }

                        await Clients.Group($"conversation_{pendingAction.ConversationId}")
                            .SendAsync("OrderCompleted", new
                            {
                                orderId = pendingAction.OrderId.Value,
                                completedBy = pendingAction.RequestedByUserId,
                                completedAt = DateTime.UtcNow
                            });
                        break;
                    }
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
        private async Task RequestWorkAction(long conversationId, long sessionId, WorkActionType actionType)
        {
            var userId = GetCurrentUserId();
            if (!await _chatService.UserHasAccessToConversationAsync(userId, conversationId))
            {
                await Clients.Caller.SendAsync("Error", "You do not have access to this conversation");
                return;
            }

            var conversation = await _chatService.GetConversationByIdAsync(conversationId);
            if (!conversation.Success || conversation.Data == null)
            {
                await Clients.Caller.SendAsync("Error", "Conversation not found");
                return;
            }

            var sessionResult = await _workSessionService.GetSessionAsync(sessionId);
            if (!sessionResult.Success || sessionResult.Data == null)
            {
                await Clients.Caller.SendAsync("Error", sessionResult.Message ?? "Session not found");
                return;
            }

            var session = sessionResult.Data;
            if (conversation.Data.Mentor?.Id != session.MentorId || conversation.Data.Student?.Id != session.StudentId)
            {
                await Clients.Caller.SendAsync("Error", "Conversation participants do not match session");
                return;
            }

            if (userId != session.MentorId && userId != session.StudentId)
            {
                await Clients.Caller.SendAsync("Error", "Invalid requester");
                return;
            }

            var targetUserId = userId == session.MentorId ? session.StudentId : session.MentorId;
            var requestId = Guid.NewGuid().ToString("N");
            var pendingAction = new PendingWorkAction
            {
                RequestId = requestId,
                ActionType = actionType,
                ConversationId = conversationId,
                OrderId = session.OrderId,
                SessionId = sessionId,
                RequestedByUserId = userId,
                TargetUserId = targetUserId,
                MentorId = session.MentorId,
                StudentId = session.StudentId,
                CreatedAtUtc = DateTime.UtcNow
            };

            lock (_pendingWorkActionsLock)
            {
                _pendingWorkActions[requestId] = pendingAction;
            }

            await Clients.Group($"conversation_{conversationId}")
                .SendAsync("WorkActionState", new
                {
                    requestId,
                    actionType = pendingAction.ActionType.ToString(),
                    status = "Waiting",
                    conversationId,
                    sessionId
                });

            await SendToUser(targetUserId, "WorkActionPopup", new
            {
                requestId,
                actionType = pendingAction.ActionType.ToString(),
                conversationId,
                sessionId,
                requestedByUserId = userId
            });

            await Clients.Caller.SendAsync("WorkActionSent", new
            {
                requestId,
                actionType = pendingAction.ActionType.ToString()
            });
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
    public class PendingWorkAction
    {
        public string RequestId { get; set; } = string.Empty;
        public WorkActionType ActionType { get; set; }
        public long ConversationId { get; set; }
        public long? OrderId { get; set; }
        public long? SessionId { get; set; }
        public long RequestedByUserId { get; set; }
        public long TargetUserId { get; set; }
        public long? MentorId { get; set; }
        public long? StudentId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public class SendGroupMessageRequest
    {
        public long GroupId { get; set; }
        public string Content { get; set; } = string.Empty;
        public MentorEdu.Models.Enums.MessageType MessageType { get; set; } = MentorEdu.Models.Enums.MessageType.Text;
    }

    public enum WorkActionType
    {
        Start,
        Pause,
        End,
        Complete
    }

    #endregion
}