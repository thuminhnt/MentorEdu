using MentorEdu.DTOs.Admin;
using MentorEdu.DTOs.Common;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using MentorEdu.Services.Common;
using MentorEdu.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Services.Implementations
{
    public class AdminOrderService : BaseService, IAdminOrderService
    {
        public AdminOrderService(IUnitOfWork unitOfWork) : base(unitOfWork) { }

        // ── Requests ──────────────────────────────────────────────────────

        public async Task<ServiceResult<PagedResponse<AdminRequestListDto>>> GetAllRequestsAsync(AdminSearchRequestDto request)
        {
            var query = _unitOfWork.Requests.Query();

            if (!string.IsNullOrWhiteSpace(request.Keyword))
                query = query.Where(r => r.Title!.Contains(request.Keyword) || r.Description!.Contains(request.Keyword));

            if (request.CategoryId.HasValue)
                query = query.Where(r => r.CategoryId == request.CategoryId);

            if (request.Status.HasValue)
                query = query.Where(r => r.Status == request.Status);

            if (request.StudentId.HasValue)
                query = query.Where(r => r.StudentId == request.StudentId);

            if (request.FromDate.HasValue)
                query = query.Where(r => r.CreatedAt >= request.FromDate);

            if (request.ToDate.HasValue)
                query = query.Where(r => r.CreatedAt <= request.ToDate);

            var totalCount = await query.CountAsync();

            var requests = await query
                .Include(r => r.Student).ThenInclude(s => s.UserProfile)
                .Include(r => r.Category)
                .Include(r => r.Proposals)
                .Include(r => r.Orders)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var dtos = requests.Select(r => new AdminRequestListDto
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description?.Length > 200 ? r.Description.Substring(0, 200) + "..." : r.Description,
                CategoryName = r.Category?.Name,
                CategoryId = r.CategoryId,
                ExpectedBudget = r.ExpectedBudget,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                ProposalCount = r.Proposals?.Count ?? 0,
                OrderCount = r.Orders?.Count ?? 0,
                StudentId = r.StudentId,
                StudentName = r.Student?.UserProfile?.FullName,
                StudentEmail = r.Student?.Email,
                StudentAvatar = r.Student?.UserProfile?.AvatarUrl
            });

            return ServiceResult<PagedResponse<AdminRequestListDto>>.SuccessResult(
                PagedResponse<AdminRequestListDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount));
        }

        public async Task<ServiceResult> AdminDeleteRequestAsync(long requestId, string reason)
        {
            var req = await _unitOfWork.Requests.Query()
                .Include(r => r.Orders)
                .Include(r => r.Proposals)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (req == null) return ServiceResult.NotFoundResult("Request not found");

            if (req.Orders?.Any(o => o.Status == OrderStatus.InProgress || o.Status == OrderStatus.PendingPayment) == true)
                return ServiceResult.FailResult("Cannot delete request with active orders");

            if (req.Proposals?.Any() == true)
                foreach (var proposal in req.Proposals)
                    _unitOfWork.Proposals.Remove(proposal);

            _unitOfWork.Requests.Remove(req);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Request deleted successfully");
        }

        // ── Proposals ─────────────────────────────────────────────────────

        public async Task<ServiceResult<PagedResponse<AdminProposalListDto>>> GetAllProposalsAsync(AdminSearchProposalDto request)
        {
            var query = _unitOfWork.Proposals.Query();

            if (request.RequestId.HasValue)
                query = query.Where(p => p.RequestId == request.RequestId);

            if (request.MentorId.HasValue)
                query = query.Where(p => p.MentorId == request.MentorId);

            if (request.Status.HasValue)
                query = query.Where(p => p.Status == request.Status);

            if (request.FromDate.HasValue)
                query = query.Where(p => p.CreatedAt >= request.FromDate);

            if (request.ToDate.HasValue)
                query = query.Where(p => p.CreatedAt <= request.ToDate);

            var totalCount = await query.CountAsync();

            var proposals = await query
                .Include(p => p.Mentor).ThenInclude(m => m.UserProfile)
                .Include(p => p.Request).ThenInclude(r => r!.Student).ThenInclude(s => s.UserProfile)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var dtos = proposals.Select(p => new AdminProposalListDto
            {
                Id = p.Id,
                RequestId = p.RequestId ?? 0,
                RequestTitle = p.Request?.Title,
                Price = p.Price,
                Message = p.Message?.Length > 200 ? p.Message.Substring(0, 200) + "..." : p.Message,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                MentorId = p.MentorId,
                MentorName = p.Mentor?.UserProfile?.FullName,
                MentorEmail = p.Mentor?.Email,
                MentorAvatar = p.Mentor?.UserProfile?.AvatarUrl,
                StudentId = p.Request?.StudentId ?? 0,
                StudentName = p.Request?.Student?.UserProfile?.FullName
            });

            return ServiceResult<PagedResponse<AdminProposalListDto>>.SuccessResult(
                PagedResponse<AdminProposalListDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount));
        }

        public async Task<ServiceResult> AdminDeleteProposalAsync(long proposalId, string reason)
        {
            var proposal = await _unitOfWork.Proposals.Query()
                .Include(p => p.Orders)
                .FirstOrDefaultAsync(p => p.Id == proposalId);

            if (proposal == null) return ServiceResult.NotFoundResult("Proposal not found");

            if (proposal.Orders?.Any(o => o.Status == OrderStatus.InProgress || o.Status == OrderStatus.PendingPayment) == true)
                return ServiceResult.FailResult("Cannot delete proposal with active orders");

            _unitOfWork.Proposals.Remove(proposal);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Proposal deleted successfully");
        }

        // ── Orders ────────────────────────────────────────────────────────

        public async Task<ServiceResult<PagedResponse<AdminOrderListDto>>> GetAllOrdersAsync(AdminSearchOrderDto request)
        {
            var query = _unitOfWork.Orders.Query();

            if (!string.IsNullOrWhiteSpace(request.Keyword))
                query = query.Where(o => o.Request!.Title!.Contains(request.Keyword));

            if (request.Status.HasValue)
                query = query.Where(o => o.Status == request.Status);

            if (request.BillingType.HasValue)
                query = query.Where(o => o.BillingType == request.BillingType);

            if (request.StudentId.HasValue)
                query = query.Where(o => o.StudentId == request.StudentId);

            if (request.MentorId.HasValue)
                query = query.Where(o => o.MentorId == request.MentorId);

            if (request.FromDate.HasValue)
                query = query.Where(o => o.CreatedAt >= request.FromDate);

            if (request.ToDate.HasValue)
                query = query.Where(o => o.CreatedAt <= request.ToDate);

            if (request.HasDispute == true)
                query = query.Where(o => o.Status == OrderStatus.Disputed);

            var totalCount = await query.CountAsync();

            var orders = await query
                .Include(o => o.Student).ThenInclude(s => s.UserProfile)
                .Include(o => o.Mentor).ThenInclude(m => m.UserProfile)
                .Include(o => o.Request)
                .Include(o => o.Escrow)
                .Include(o => o.Reports)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var dtos = orders.Select(o => MapToOrderListDto(o));

            return ServiceResult<PagedResponse<AdminOrderListDto>>.SuccessResult(
                PagedResponse<AdminOrderListDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount));
        }

        public async Task<ServiceResult<AdminOrderListDto>> GetOrderDetailForAdminAsync(long orderId)
        {
            var order = await _unitOfWork.Orders.Query()
                .Include(o => o.Student).ThenInclude(s => s.UserProfile)
                .Include(o => o.Mentor).ThenInclude(m => m.UserProfile)
                .Include(o => o.Request)
                .Include(o => o.Escrow)
                .Include(o => o.Reports)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return ServiceResult<AdminOrderListDto>.NotFoundResult("Order not found");

            return ServiceResult<AdminOrderListDto>.SuccessResult(MapToOrderListDto(order));
        }

        public async Task<ServiceResult> AdminUpdateOrderStatusAsync(long orderId, long adminId, AdminUpdateOrderStatusDto request)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null) return ServiceResult.NotFoundResult("Order not found");

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

            await _unitOfWork.OrderDetails.AddAsync(new OrderDetail
            {
                OrderId = orderId,
                OldStatus = oldStatus.ToString(),
                NewStatus = request.NewStatus.ToString(),
                ChangedBy = adminId,
                Note = $"[Admin Action] {request.Note}",
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Order status updated successfully");
        }

        public async Task<ServiceResult> AdminRefundOrderAsync(long orderId, long adminId, AdminRefundOrderDto request)
        {
            var order = await _unitOfWork.Orders.Query()
                .Include(o => o.Escrow)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return ServiceResult.NotFoundResult("Order not found");

            if (order.Escrow == null || order.Escrow.Status != EscrowStatus.Holding)
                return ServiceResult.FailResult("No escrow to refund");

            return await _unitOfWork.ExecuteWithStrategyAsync(async () =>
            {
                var refundAmount = request.FullRefund ? order.Escrow!.Amount : request.Amount;

                if (refundAmount > order.Escrow!.Amount)
                    return ServiceResult.FailResult("Refund amount exceeds escrow amount");

                order.Escrow.Status = EscrowStatus.Refunded;
                order.Escrow.RefundedAt = DateTime.UtcNow;
                order.Status = OrderStatus.Cancelled;
                order.CancelledAt = DateTime.UtcNow;
                order.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Orders.Update(order);

                var studentWallet = await _unitOfWork.Wallets.GetByUserIdAsync(order.StudentId);
                if (studentWallet != null)
                {
                    studentWallet.Balance += refundAmount;
                    studentWallet.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Wallets.Update(studentWallet);

                    await _unitOfWork.Transactions.AddAsync(new Transaction
                    {
                        WalletId = studentWallet.Id,
                        OrderId = orderId,
                        Type = TransactionType.EscrowRefund,
                        Amount = refundAmount,
                        Status = TransactionStatus.Success,
                        Description = $"[Admin Refund] {request.Reason}",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _unitOfWork.OrderDetails.AddAsync(new OrderDetail
                {
                    OrderId = orderId,
                    OldStatus = order.Status.ToString(),
                    NewStatus = OrderStatus.Cancelled.ToString(),
                    ChangedBy = adminId,
                    Note = $"[Admin Refund] Amount: {refundAmount:N0} VND. Reason: {request.Reason}",
                    CreatedAt = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                return ServiceResult.SuccessResult($"Refund of {refundAmount:N0} VND processed successfully");
            });
        }

        // ── Transactions ──────────────────────────────────────────────────

        public async Task<ServiceResult<PagedResponse<AdminTransactionListDto>>> GetAllTransactionsAsync(AdminSearchTransactionDto request)
        {
            var query = _unitOfWork.Transactions.Query();

            if (request.UserId.HasValue)
                query = query.Where(t => t.Wallet.UserId == request.UserId);

            if (request.Type.HasValue)
                query = query.Where(t => t.Type == request.Type);

            if (request.Status.HasValue)
                query = query.Where(t => t.Status == request.Status);

            if (request.FromDate.HasValue)
                query = query.Where(t => t.CreatedAt >= request.FromDate);

            if (request.ToDate.HasValue)
                query = query.Where(t => t.CreatedAt <= request.ToDate);

            if (request.MinAmount.HasValue)
                query = query.Where(t => t.Amount >= request.MinAmount);

            if (request.MaxAmount.HasValue)
                query = query.Where(t => t.Amount <= request.MaxAmount);

            var totalCount = await query.CountAsync();

            var transactions = await query
                .Include(t => t.Wallet).ThenInclude(w => w.User).ThenInclude(u => u.UserProfile)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var dtos = transactions.Select(t => new AdminTransactionListDto
            {
                Id = t.Id,
                WalletId = t.WalletId,
                UserId = t.Wallet?.UserId ?? 0,
                UserName = t.Wallet?.User?.UserProfile?.FullName,
                UserEmail = t.Wallet?.User?.Email,
                OrderId = t.OrderId,
                Type = t.Type,
                Amount = t.Amount,
                ExternalRef = t.ExternalRef,
                Status = t.Status,
                Description = t.Description,
                CreatedAt = t.CreatedAt
            });

            return ServiceResult<PagedResponse<AdminTransactionListDto>>.SuccessResult(
                PagedResponse<AdminTransactionListDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount));
        }

        public async Task<ServiceResult> AdminVerifyTransactionAsync(long transactionId, long adminId, bool approve, string? note)
        {
            var transaction = await _unitOfWork.Transactions.GetByIdAsync(transactionId);
            if (transaction == null) return ServiceResult.NotFoundResult("Transaction not found");

            if (transaction.Status != TransactionStatus.Pending)
                return ServiceResult.FailResult("Only pending transactions can be verified");

            transaction.Status = approve ? TransactionStatus.Success : TransactionStatus.Failed;
            transaction.Description = $"{transaction.Description} | [Admin: {(approve ? "Approved" : "Rejected")}] {note}";
            _unitOfWork.Transactions.Update(transaction);

            if (approve && transaction.Type == TransactionType.Topup)
            {
                var wallet = await _unitOfWork.Wallets.GetByIdAsync(transaction.WalletId);
                if (wallet != null)
                {
                    wallet.Balance += transaction.Amount;
                    wallet.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Wallets.Update(wallet);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult($"Transaction {(approve ? "approved" : "rejected")} successfully");
        }

        // ── Wallets ───────────────────────────────────────────────────────

        public async Task<ServiceResult<PagedResponse<AdminWalletListDto>>> GetAllWalletsAsync(AdminSearchWalletDto request)
        {
            var query = _unitOfWork.Wallets.Query();

            if (request.UserId.HasValue)
                query = query.Where(w => w.UserId == request.UserId);

            if (!string.IsNullOrWhiteSpace(request.UserEmail))
                query = query.Where(w => w.User.Email.Contains(request.UserEmail));

            if (request.MinBalance.HasValue)
                query = query.Where(w => w.Balance >= request.MinBalance);

            if (request.MaxBalance.HasValue)
                query = query.Where(w => w.Balance <= request.MaxBalance);

            var totalCount = await query.CountAsync();

            var wallets = await query
                .Include(w => w.User).ThenInclude(u => u.UserProfile)
                .Include(w => w.Transactions)
                .OrderByDescending(w => w.Balance)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var dtos = wallets.Select(w => new AdminWalletListDto
            {
                Id = w.Id,
                UserId = w.UserId,
                UserName = w.User?.UserProfile?.FullName,
                UserEmail = w.User?.Email,
                UserRole = w.User?.Role.ToString(),
                Balance = w.Balance,
                LockedBalance = w.LockedBalance,
                Currency = w.Currency,
                CreatedAt = w.CreatedAt,
                UpdatedAt = w.UpdatedAt,
                TransactionCount = w.Transactions?.Count ?? 0
            });

            return ServiceResult<PagedResponse<AdminWalletListDto>>.SuccessResult(
                PagedResponse<AdminWalletListDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount));
        }

        public async Task<ServiceResult<AdminWalletListDto>> GetWalletDetailForAdminAsync(long walletId)
        {
            var wallet = await _unitOfWork.Wallets.Query()
                .Include(w => w.User).ThenInclude(u => u.UserProfile)
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.Id == walletId);

            if (wallet == null)
                return ServiceResult<AdminWalletListDto>.NotFoundResult("Wallet not found");

            return ServiceResult<AdminWalletListDto>.SuccessResult(new AdminWalletListDto
            {
                Id = wallet.Id,
                UserId = wallet.UserId,
                UserName = wallet.User?.UserProfile?.FullName,
                UserEmail = wallet.User?.Email,
                UserRole = wallet.User?.Role.ToString(),
                Balance = wallet.Balance,
                LockedBalance = wallet.LockedBalance,
                Currency = wallet.Currency,
                CreatedAt = wallet.CreatedAt,
                UpdatedAt = wallet.UpdatedAt,
                TransactionCount = wallet.Transactions?.Count ?? 0
            });
        }

        // ── Reviews ───────────────────────────────────────────────────────

        public async Task<ServiceResult<PagedResponse<AdminReviewListDto>>> GetAllReviewsAsync(AdminSearchReviewDto request)
        {
            var query = _unitOfWork.Reviews.Query();

            if (request.OrderId.HasValue)
                query = query.Where(r => r.OrderId == request.OrderId);

            if (request.FromUserId.HasValue)
                query = query.Where(r => r.FromUserId == request.FromUserId);

            if (request.ToUserId.HasValue)
                query = query.Where(r => r.ToUserId == request.ToUserId);

            if (request.MinRating.HasValue)
                query = query.Where(r => r.Rating >= request.MinRating);

            if (request.MaxRating.HasValue)
                query = query.Where(r => r.Rating <= request.MaxRating);

            if (request.FromDate.HasValue)
                query = query.Where(r => r.CreatedAt >= request.FromDate);

            if (request.ToDate.HasValue)
                query = query.Where(r => r.CreatedAt <= request.ToDate);

            var totalCount = await query.CountAsync();

            var reviews = await query
                .Include(r => r.FromUser).ThenInclude(u => u.UserProfile)
                .Include(r => r.ToUser).ThenInclude(u => u.UserProfile)
                .Include(r => r.Order).ThenInclude(o => o.Request)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var dtos = reviews.Select(r => new AdminReviewListDto
            {
                Id = r.Id,
                OrderId = r.OrderId,
                OrderTitle = r.Order?.Request?.Title,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                FromUserId = r.FromUserId,
                FromUserName = r.FromUser?.UserProfile?.FullName,
                FromUserEmail = r.FromUser?.Email,
                ToUserId = r.ToUserId,
                ToUserName = r.ToUser?.UserProfile?.FullName,
                ToUserEmail = r.ToUser?.Email
            });

            return ServiceResult<PagedResponse<AdminReviewListDto>>.SuccessResult(
                PagedResponse<AdminReviewListDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount));
        }

        public async Task<ServiceResult> AdminDeleteReviewAsync(long reviewId, string reason)
        {
            var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
            if (review == null) return ServiceResult.NotFoundResult("Review not found");

            var mentorProfile = await _unitOfWork.MentorProfiles.GetByUserIdAsync(review.ToUserId);
            if (mentorProfile != null && mentorProfile.RatingCount > 0)
            {
                var remainingReviews = await _unitOfWork.Reviews.Query()
                    .Where(r => r.ToUserId == review.ToUserId && r.Id != reviewId)
                    .ToListAsync();

                if (remainingReviews.Any())
                {
                    mentorProfile.RatingAvg = (decimal)remainingReviews.Average(r => r.Rating);
                    mentorProfile.RatingCount = remainingReviews.Count;
                }
                else
                {
                    mentorProfile.RatingAvg = 0;
                    mentorProfile.RatingCount = 0;
                }
                mentorProfile.UpdatedAt = DateTime.UtcNow;
            }

            _unitOfWork.Reviews.Remove(review);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Review deleted successfully");
        }

        // ── Private helpers ───────────────────────────────────────────────

        private static AdminOrderListDto MapToOrderListDto(Order o) => new()
        {
            Id = o.Id,
            RequestTitle = o.Request?.Title,
            BillingType = o.BillingType,
            AgreedPrice = o.AgreedPrice,
            AgreedHourlyRate = o.AgreedHourlyRate,
            TotalPrice = o.TotalPrice,
            Status = o.Status,
            StartedAt = o.StartedAt,
            CompletedAt = o.CompletedAt,
            CancelledAt = o.CancelledAt,
            CreatedAt = o.CreatedAt,
            StudentId = o.StudentId,
            StudentName = o.Student?.UserProfile?.FullName,
            StudentEmail = o.Student?.Email,
            MentorId = o.MentorId,
            MentorName = o.Mentor?.UserProfile?.FullName,
            MentorEmail = o.Mentor?.Email,
            HasEscrow = o.Escrow != null,
            EscrowAmount = o.Escrow?.Amount,
            EscrowStatus = o.Escrow?.Status.ToString(),
            ReportCount = o.Reports?.Count ?? 0
        };
    }
}
