using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Order;
using MentorEdu.DTOs.Review;
using MentorEdu.DTOs.User;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using MentorEdu.Services.Common;
using MentorEdu.Services.Interfaces;

namespace MentorEdu.Services.Implementations
{
    public class OrderService : BaseService, IOrderService
    {
        private readonly INotificationService _notificationService;

        public OrderService(IUnitOfWork unitOfWork, INotificationService notificationService) : base(unitOfWork)
        {
            _notificationService = notificationService;
        }

        #region Order Management

        public async Task<ServiceResult<OrderDetailDto>> GetByIdAsync(long orderId, long userId)
        {
            var order = await _unitOfWork.Orders.GetByIdWithDetailsAsync(orderId);
            
            if (order == null)
                return ServiceResult<OrderDetailDto>.NotFoundResult("Order not found");

            // Check permission
            if (order.StudentId != userId && order.MentorId != userId)
                return ServiceResult<OrderDetailDto>.ForbiddenResult("You do not have permission to view this order");

            var dto = await MapToDetailDto(order);
            return ServiceResult<OrderDetailDto>.SuccessResult(dto);
        }

        public async Task<ServiceResult<PagedResponse<OrderListDto>>> GetMyOrdersAsync(long userId, bool isMentor, SearchOrderDto searchDto)
        {
            var (items, totalCount) = await _unitOfWork.Orders.GetPagedByUserAsync(
                userId, isMentor, searchDto.PageNumber, searchDto.PageSize, searchDto.Status);

            var dtos = items.Select(MapToListDto);
            var response = PagedResponse<OrderListDto>.Create(dtos, searchDto.PageNumber, searchDto.PageSize, totalCount);

            return ServiceResult<PagedResponse<OrderListDto>>.SuccessResult(response);
        }

        public async Task<ServiceResult> UpdateOrderStatusAsync(long orderId, long userId, UpdateOrderStatusDto request)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            
            if (order == null)
                return ServiceResult.NotFoundResult("Order not found");

            if (order.StudentId != userId && order.MentorId != userId)
                return ServiceResult.ForbiddenResult("You do not have permission to update this order");

            var oldStatus = order.Status;
            order.Status = request.NewStatus;
            order.UpdatedAt = DateTime.UtcNow;

            if (request.NewStatus == OrderStatus.InProgress && order.StartedAt == null)
                order.StartedAt = DateTime.UtcNow;

            if (request.NewStatus == OrderStatus.Completed)
                order.CompletedAt = DateTime.UtcNow;

            if (request.NewStatus == OrderStatus.Cancelled)
                order.CancelledAt = DateTime.UtcNow;

            _unitOfWork.Orders.Update(order);

            // Log status change
            var orderDetail = new OrderDetail
            {
                OrderId = orderId,
                OldStatus = oldStatus.ToString(),
                NewStatus = request.NewStatus.ToString(),
                ChangedBy = userId,
                Note = request.Note,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.OrderDetails.AddAsync(orderDetail);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Status updated successfully");
        }

        public async Task<ServiceResult> CancelOrderAsync(long orderId, long userId, string? reason = null)
        {
            var order = await _unitOfWork.Orders.GetByIdWithDetailsAsync(orderId);
            
            if (order == null)
                return ServiceResult.NotFoundResult("Order not found");

            if (order.StudentId != userId && order.MentorId != userId)
                return ServiceResult.ForbiddenResult("You do not have permission to cancel this order");

            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
                return ServiceResult.FailResult("Cannot cancel this order");

            return await _unitOfWork.ExecuteWithStrategyAsync(async () =>
            {
                var oldStatus = order.Status;
                order.Status = OrderStatus.Cancelled;
                order.CancelledAt = DateTime.UtcNow;
                order.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Orders.Update(order);

                // Refund escrow if exists
                if (order.Escrow != null && order.Escrow.Status == EscrowStatus.Holding)
                {
                    order.Escrow.Status = EscrowStatus.Refunded;
                    order.Escrow.RefundedAt = DateTime.UtcNow;

                    // Return money to student wallet
                    var studentWallet = await _unitOfWork.Wallets.GetByUserIdAsync(order.StudentId);
                    if (studentWallet != null)
                    {
                        studentWallet.Balance += order.Escrow.Amount;
                        studentWallet.UpdatedAt = DateTime.UtcNow;
                        _unitOfWork.Wallets.Update(studentWallet);

                        // Create refund transaction
                        var transaction = new Transaction
                        {
                            WalletId = studentWallet.Id,
                            OrderId = orderId,
                            Type = TransactionType.EscrowRefund,
                            Amount = order.Escrow.Amount,
                            Status = TransactionStatus.Success,
                            Description = $"Refund for order #{orderId}",
                            CreatedAt = DateTime.UtcNow
                        };

                        await _unitOfWork.Transactions.AddAsync(transaction);
                    }
                }

                // Log status change
                var orderDetail = new OrderDetail
                {
                    OrderId = orderId,
                    OldStatus = oldStatus.ToString(),
                    NewStatus = OrderStatus.Cancelled.ToString(),
                    ChangedBy = userId,
                    Note = reason ?? "Order cancelled",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.OrderDetails.AddAsync(orderDetail);
                await _unitOfWork.SaveChangesAsync();

                return ServiceResult.SuccessResult("Order cancelled successfully");
            });
        }

        public async Task<ServiceResult> CompleteOrderAsync(long orderId, long userId)
        {
            var order = await _unitOfWork.Orders.GetByIdWithDetailsAsync(orderId);
            
            if (order == null)
                return ServiceResult.NotFoundResult("Order not found");

            if (order.StudentId != userId && order.MentorId != userId)
                return ServiceResult.ForbiddenResult("You do not have permission to complete this order");

            if (order.Status != OrderStatus.InProgress)
                return ServiceResult.FailResult("Only in-progress orders can be completed");

            return await _unitOfWork.ExecuteWithStrategyAsync(async () =>
            {
                var oldStatus = order.Status;
                order.Status = OrderStatus.Completed;
                order.CompletedAt = DateTime.UtcNow;
                order.UpdatedAt = DateTime.UtcNow;

                // Calculate total price based on billing type
                if (order.BillingType == BillingType.Hourly)
                {
                    var totalMinutes = await _unitOfWork.WorkSessions.GetTotalMinutesByOrderAsync(orderId);
                    var totalHours = (decimal)totalMinutes / 60;
                    order.TotalPrice = totalHours * (order.AgreedHourlyRate ?? 0);
                }
                else
                {
                    order.TotalPrice = order.AgreedPrice;
                }

                _unitOfWork.Orders.Update(order);

                // Release escrow to mentor
                if (order.Escrow != null && order.Escrow.Status == EscrowStatus.Holding)
                {
                    order.Escrow.Status = EscrowStatus.Released;
                    order.Escrow.ReleasedAt = DateTime.UtcNow;

                    // Transfer money to mentor wallet
                    var mentorWallet = await _unitOfWork.Wallets.GetByUserIdAsync(order.MentorId);
                    if (mentorWallet != null)
                    {
                        var amountToTransfer = order.TotalPrice ?? order.Escrow.Amount;
                        mentorWallet.Balance += amountToTransfer;
                        mentorWallet.UpdatedAt = DateTime.UtcNow;
                        _unitOfWork.Wallets.Update(mentorWallet);

                        // Create release transaction
                        var transaction = new Transaction
                        {
                            WalletId = mentorWallet.Id,
                            OrderId = orderId,
                            Type = TransactionType.EscrowRelease,
                            Amount = amountToTransfer,
                            Status = TransactionStatus.Success,
                            Description = $"Payment for order #{orderId}",
                            CreatedAt = DateTime.UtcNow
                        };

                        await _unitOfWork.Transactions.AddAsync(transaction);
                    }
                }

                // Log status change
                var orderDetail = new OrderDetail
                {
                    OrderId = orderId,
                    OldStatus = oldStatus.ToString(),
                    NewStatus = OrderStatus.Completed.ToString(),
                    ChangedBy = userId,
                    Note = "Order completed",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.OrderDetails.AddAsync(orderDetail);
                await _unitOfWork.SaveChangesAsync();

                return ServiceResult.SuccessResult("Order completed successfully");
            });
        }

        public async Task<ServiceResult> DisputeOrderAsync(long orderId, long userId, string reason)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            
            if (order == null)
                return ServiceResult.NotFoundResult("Order not found");

            if (order.StudentId != userId && order.MentorId != userId)
                return ServiceResult.ForbiddenResult("You do not have permission to create a dispute");

            if (order.Status != OrderStatus.InProgress)
                return ServiceResult.FailResult("Only in-progress orders can be disputed");

            var oldStatus = order.Status;
            order.Status = OrderStatus.Disputed;
            order.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Orders.Update(order);

            // Create report
            var reportedUserId = userId == order.StudentId ? order.MentorId : order.StudentId;
            var report = new Report
            {
                ReporterId = userId,
                ReportedUserId = reportedUserId,
                OrderId = orderId,
                Reason = reason,
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Reports.AddAsync(report);

            // Log status change
            var orderDetail = new OrderDetail
            {
                OrderId = orderId,
                OldStatus = oldStatus.ToString(),
                NewStatus = OrderStatus.Disputed.ToString(),
                ChangedBy = userId,
                Note = reason,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.OrderDetails.AddAsync(orderDetail);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Dispute created successfully. Admin will review.");
        }

        #endregion

        #region Work Sessions

        public async Task<ServiceResult<WorkSessionDetailDto>> StartWorkSessionAsync(long userId, StartWorkSessionDto request)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(request.OrderId);
            
            if (order == null)
                return ServiceResult<WorkSessionDetailDto>.NotFoundResult("Order not found");

            if (order.MentorId != userId)
                return ServiceResult<WorkSessionDetailDto>.ForbiddenResult("Only the mentor can start a session");

            if (order.Status != OrderStatus.InProgress)
            {
                if (order.Status == OrderStatus.PendingPayment)
                {
                    return ServiceResult<WorkSessionDetailDto>.FailResult("Cannot start session - payment has not been completed yet.");
                }
                return ServiceResult<WorkSessionDetailDto>.FailResult("Order is not in progress. Cannot start a session.");
            }

            // Check for active session
            var activeSession = await _unitOfWork.WorkSessions.GetActiveSessionByOrderAsync(request.OrderId);
            if (activeSession != null)
                return ServiceResult<WorkSessionDetailDto>.FailResult("A session is already in progress");

            var session = new WorkSession
            {
                OrderId = request.OrderId,
                MentorId = order.MentorId,
                StudentId = order.StudentId,
                StartTime = DateTime.UtcNow,
                Status = WorkSessionStatus.Running,
                TotalMinutes = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.WorkSessions.AddAsync(session);
            await _unitOfWork.SaveChangesAsync();

            // Create first tracking
            var tracking = new SessionTracking
            {
                SessionId = session.Id,
                StartAt = DateTime.UtcNow
            };

            await _unitOfWork.SessionTrackings.AddAsync(tracking);
            await _unitOfWork.SaveChangesAsync();

            var dto = await MapToWorkSessionDetailDto(session);
            return ServiceResult<WorkSessionDetailDto>.SuccessResult(dto, "Session started successfully");
        }

        public async Task<ServiceResult<WorkSessionDetailDto>> EndWorkSessionAsync(long userId, EndWorkSessionDto request)
        {
            var session = await _unitOfWork.WorkSessions.GetByIdWithTrackingsAsync(request.SessionId);
            
            if (session == null)
                return ServiceResult<WorkSessionDetailDto>.NotFoundResult("Session not found");

            if (session.MentorId != userId)
                return ServiceResult<WorkSessionDetailDto>.ForbiddenResult("Only the mentor can end the session");

            if (session.Status != WorkSessionStatus.Running)
                return ServiceResult<WorkSessionDetailDto>.FailResult("Session is not running");

            // End active tracking
            var activeTracking = await _unitOfWork.SessionTrackings.GetActiveTrackingBySessionAsync(request.SessionId);
            if (activeTracking != null)
            {
                activeTracking.EndAt = DateTime.UtcNow;
                activeTracking.DurationMin = (int)(DateTime.UtcNow - (activeTracking.StartAt ?? DateTime.UtcNow)).TotalMinutes;
            }

            // Calculate total minutes
            var trackings = await _unitOfWork.SessionTrackings.GetBySessionIdAsync(request.SessionId);
            session.TotalMinutes = trackings.Sum(t => t.DurationMin ?? 0);
            session.EndTime = DateTime.UtcNow;
            session.Status = request.Pause ? WorkSessionStatus.Paused : WorkSessionStatus.Finished;
            session.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.WorkSessions.Update(session);
            await _unitOfWork.SaveChangesAsync();

            var dto = await MapToWorkSessionDetailDto(session);
            var message = request.Pause ? "Session paused successfully" : "Session ended successfully";
            
            return ServiceResult<WorkSessionDetailDto>.SuccessResult(dto, message);
        }

        public async Task<ServiceResult<WorkSessionDetailDto>> ResumeWorkSessionAsync(long userId, ResumeWorkSessionDto request)
        {
            var session = await _unitOfWork.WorkSessions.GetByIdWithTrackingsAsync(request.SessionId);
            
            if (session == null)
                return ServiceResult<WorkSessionDetailDto>.NotFoundResult("Session not found");

            if (session.MentorId != userId)
                return ServiceResult<WorkSessionDetailDto>.ForbiddenResult("Only the mentor can resume the session");

            if (session.Status != WorkSessionStatus.Paused)
                return ServiceResult<WorkSessionDetailDto>.FailResult("Session is not paused");

            session.Status = WorkSessionStatus.Running;
            session.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.WorkSessions.Update(session);

            // Create new tracking
            var tracking = new SessionTracking
            {
                SessionId = session.Id,
                StartAt = DateTime.UtcNow
            };

            await _unitOfWork.SessionTrackings.AddAsync(tracking);
            await _unitOfWork.SaveChangesAsync();

            var dto = await MapToWorkSessionDetailDto(session);
            return ServiceResult<WorkSessionDetailDto>.SuccessResult(dto, "Session resumed successfully");
        }

        public async Task<ServiceResult<WorkSessionSummaryDto>> GetWorkSessionSummaryAsync(long orderId)
        {
            var sessions = await _unitOfWork.WorkSessions.GetByOrderIdAsync(orderId);
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);

            var totalMinutes = sessions.Where(s => s.Status == WorkSessionStatus.Finished).Sum(s => s.TotalMinutes ?? 0);
            var currentSession = sessions.FirstOrDefault(s => s.Status == WorkSessionStatus.Running || s.Status == WorkSessionStatus.Paused);

            var summary = new WorkSessionSummaryDto
            {
                OrderId = orderId,
                OrderStatus = order?.Status.ToString(),
                TotalSessions = sessions.Count(s => s.Status == WorkSessionStatus.Finished),
                TotalMinutes = totalMinutes,
                EstimatedCost = order?.BillingType == BillingType.Hourly 
                    ? (decimal)totalMinutes / 60 * (order.AgreedHourlyRate ?? 0)
                    : order?.AgreedPrice,
                CurrentSession = currentSession != null ? new WorkSessionListDto
                {
                    Id = currentSession.Id,
                    OrderId = currentSession.OrderId,
                    StartTime = currentSession.StartTime,
                    EndTime = currentSession.EndTime,
                    TotalMinutes = currentSession.TotalMinutes,
                    Status = currentSession.Status,
                    CreatedAt = currentSession.CreatedAt
                } : null
            };

            return ServiceResult<WorkSessionSummaryDto>.SuccessResult(summary);
        }

        #endregion

        #region Reviews

        public async Task<ServiceResult<ReviewDto>> CreateReviewAsync(long userId, CreateReviewDto request)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(request.OrderId);
            
            if (order == null)
                return ServiceResult<ReviewDto>.NotFoundResult("Order not found");

            if (order.Status != OrderStatus.Completed)
                return ServiceResult<ReviewDto>.FailResult("Only completed orders can be reviewed");

            if (order.StudentId != userId && order.MentorId != userId)
                return ServiceResult<ReviewDto>.ForbiddenResult("You do not have permission to review this order");

            // Check if already reviewed
            var hasReviewed = await _unitOfWork.Reviews.HasReviewedAsync(request.OrderId, userId);
            if (hasReviewed)
                return ServiceResult<ReviewDto>.FailResult("You have already reviewed this order");

            var toUserId = userId == order.StudentId ? order.MentorId : order.StudentId;

            var review = new Review
            {
                OrderId = request.OrderId,
                FromUserId = userId,
                ToUserId = toUserId,
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Reviews.AddAsync(review);

            // Update mentor rating if review is for mentor
            if (toUserId == order.MentorId)
            {
                var mentorProfile = await _unitOfWork.MentorProfiles.GetByUserIdAsync(order.MentorId);
                if (mentorProfile != null)
                {
                    var existingAvg = await _unitOfWork.Reviews.GetAverageRatingByUserAsync(order.MentorId);
                    var existingCount = await _unitOfWork.Reviews.CountAsync(r => r.ToUserId == order.MentorId);

                    // Include the new review in the calculation before SaveChanges
                    var newCount = existingCount + 1;
                    var newAvg = ((double)existingAvg * existingCount + request.Rating) / newCount;

                    mentorProfile.RatingAvg = (decimal)newAvg;
                    mentorProfile.RatingCount = (int)newCount;
                    mentorProfile.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _unitOfWork.SaveChangesAsync();

            // Notify mentor about the new review
            if (toUserId == order.MentorId)
            {
                var reviewer = await _unitOfWork.Users.GetByIdWithProfilesAsync(userId);
                var reviewerName = reviewer?.UserProfile?.FullName ?? reviewer?.Email ?? "A student";
                await _notificationService.CreateNotificationAsync(
                    order.MentorId,
                    NotificationType.NewReview,
                    "You received a new review",
                    $"{reviewerName} gave you {request.Rating} star{(request.Rating == 1 ? "" : "s")}.",
                    System.Text.Json.JsonSerializer.Serialize(new { orderId = order.Id, reviewId = review.Id })
                );
            }

            var dto = new ReviewDto
            {
                Id = review.Id,
                OrderId = review.OrderId,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
                FromUserId = review.FromUserId,
                ToUserId = review.ToUserId
            };

            return ServiceResult<ReviewDto>.CreatedResult(dto, "Review submitted successfully");
        }

        public async Task<ServiceResult<List<ReviewDto>>> GetOrderReviewsAsync(long orderId)
        {
            var reviews = await _unitOfWork.Reviews.GetByOrderIdAsync(orderId);

            var dtos = reviews.Select(r => new ReviewDto
            {
                Id = r.Id,
                OrderId = r.OrderId,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                FromUserId = r.FromUserId,
                FromUserName = r.FromUser?.UserProfile?.FullName,
                FromUserAvatar = r.FromUser?.UserProfile?.AvatarUrl,
                ToUserId = r.ToUserId,
                ToUserName = r.ToUser?.UserProfile?.FullName
            }).ToList();

            return ServiceResult<List<ReviewDto>>.SuccessResult(dtos);
        }

        #endregion

        #region Private Methods

        private OrderListDto MapToListDto(Order order)
        {
            return new OrderListDto
            {
                Id = order.Id,
                RequestTitle = order.Request?.Title,
                BillingType = order.BillingType,
                AgreedPrice = order.AgreedPrice,
                AgreedHourlyRate = order.AgreedHourlyRate,
                TotalPrice = order.TotalPrice,
                Status = order.Status,
                StartedAt = order.StartedAt,
                CompletedAt = order.CompletedAt,
                CreatedAt = order.CreatedAt,
                StudentId = order.StudentId,
                StudentName = order.Student?.UserProfile?.FullName,
                StudentAvatar = order.Student?.UserProfile?.AvatarUrl,
                MentorId = order.MentorId,
                MentorName = order.Mentor?.UserProfile?.FullName,
                MentorAvatar = order.Mentor?.UserProfile?.AvatarUrl,
                HasEscrow = order.Escrow != null,
                IsReviewed = order.Reviews != null && order.Reviews.Any()
            };
        }

        private async Task<OrderDetailDto> MapToDetailDto(Order order)
        {
            var statusHistory = await _unitOfWork.OrderDetails.GetByOrderIdAsync(order.Id);

            return new OrderDetailDto
            {
                Id = order.Id,
                RequestId = order.RequestId,
                RequestTitle = order.Request?.Title,
                ProposalId = order.ProposalId,
                BillingType = order.BillingType,
                AgreedPrice = order.AgreedPrice,
                AgreedHourlyRate = order.AgreedHourlyRate,
                TotalPrice = order.TotalPrice,
                Status = order.Status,
                StartedAt = order.StartedAt,
                CompletedAt = order.CompletedAt,
                CancelledAt = order.CancelledAt,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Student = new UserBasicDto
                {
                    Id = order.Student.Id,
                    Email = order.Student.Email,
                    FullName = order.Student.UserProfile?.FullName,
                    AvatarUrl = order.Student.UserProfile?.AvatarUrl,
                    Role = order.Student.Role
                },
                Mentor = new MentorListDto
                {
                    Id = order.Mentor.Id,
                    FullName = order.Mentor.UserProfile?.FullName,
                    AvatarUrl = order.Mentor.UserProfile?.AvatarUrl,
                    Title = order.Mentor.MentorProfile?.Title,
                    RatingAvg = order.Mentor.MentorProfile?.RatingAvg,
                    RatingCount = order.Mentor.MentorProfile?.RatingCount
                },
                Escrow = order.Escrow != null ? new EscrowDto
                {
                    Id = order.Escrow.Id,
                    Amount = order.Escrow.Amount,
                    Status = order.Escrow.Status,
                    CreatedAt = order.Escrow.CreatedAt,
                    ReleasedAt = order.Escrow.ReleasedAt,
                    RefundedAt = order.Escrow.RefundedAt
                } : null,
                WorkSessions = order.WorkSessions?.Select(ws => new WorkSessionListDto
                {
                    Id = ws.Id,
                    OrderId = ws.OrderId,
                    StartTime = ws.StartTime,
                    EndTime = ws.EndTime,
                    TotalMinutes = ws.TotalMinutes,
                    Status = ws.Status,
                    CreatedAt = ws.CreatedAt
                }).ToList(),
                TotalSessionMinutes = order.WorkSessions?.Where(ws => ws.Status == WorkSessionStatus.Finished).Sum(ws => ws.TotalMinutes ?? 0) ?? 0,
                StatusHistory = statusHistory.Select(sh => new OrderStatusHistoryDto
                {
                    Id = sh.Id,
                    OldStatus = sh.OldStatus,
                    NewStatus = sh.NewStatus,
                    ChangedByName = sh.ChangedByUser?.UserProfile?.FullName,
                    Note = sh.Note,
                    CreatedAt = sh.CreatedAt
                }).ToList()
            };
        }

        private async Task<WorkSessionDetailDto> MapToWorkSessionDetailDto(WorkSession session)
        {
            var trackings = await _unitOfWork.SessionTrackings.GetBySessionIdAsync(session.Id);

            return new WorkSessionDetailDto
            {
                Id = session.Id,
                OrderId = session.OrderId,
                MentorId = session.MentorId,
                MentorName = session.Mentor?.UserProfile?.FullName,
                StudentId = session.StudentId,
                StudentName = session.Student?.UserProfile?.FullName,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                TotalMinutes = session.TotalMinutes,
                Status = session.Status,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt,
                Trackings = trackings.Select(t => new SessionTrackingDto
                {
                    Id = t.Id,
                    StartAt = t.StartAt,
                    EndAt = t.EndAt,
                    DurationMin = t.DurationMin
                }).ToList()
            };
        }

        #endregion
    }
}
