using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using MentorEdu.Services.Common;
using MentorEdu.Services.Interfaces;

namespace MentorEdu.Services.Implementations
{
    public class AdminStatsService : BaseService, IAdminStatsService
    {
        public AdminStatsService(IUnitOfWork unitOfWork) : base(unitOfWork) { }

        public async Task<ServiceResult<AdminDashboardDto>> GetDashboardAsync()
        {
            var totalUsers = await _unitOfWork.Users.CountAsync();
            var totalStudents = await _unitOfWork.Users.CountAsync(u => u.Role == UserRole.Student);
            var totalMentors = await _unitOfWork.Users.CountAsync(u => u.Role == UserRole.Mentor);
            var totalOrders = await _unitOfWork.Orders.CountAsync();
            var activeOrders = await _unitOfWork.Orders.CountAsync(o => o.Status == OrderStatus.InProgress);
            var completedOrders = await _unitOfWork.Orders.CountAsync(o => o.Status == OrderStatus.Completed);
            var disputedOrders = await _unitOfWork.Orders.CountAsync(o => o.Status == OrderStatus.Disputed);
            var pendingReports = await _unitOfWork.Reports.CountAsync(r => r.Status == null || r.Status == "pending");
            var totalRequests = await _unitOfWork.Requests.CountAsync();
            var totalProposals = await _unitOfWork.Proposals.CountAsync();
            var totalPosts = await _unitOfWork.CommunityPosts.CountAsync();
            var totalEscrowHolding = await _unitOfWork.Escrows.GetTotalHoldingAmountAsync();

            var completedOrdersList = await _unitOfWork.Orders.GetCompletedOrdersAsync();
            var totalRevenue = completedOrdersList.Sum(o => o.TotalPrice ?? 0);

            var dashboard = new AdminDashboardDto
            {
                TotalUsers = totalUsers,
                TotalStudents = totalStudents,
                TotalMentors = totalMentors,
                TotalOrders = totalOrders,
                ActiveOrders = activeOrders,
                CompletedOrders = completedOrders,
                DisputedOrders = disputedOrders,
                PendingReports = pendingReports,
                TotalRequests = totalRequests,
                TotalProposals = totalProposals,
                TotalPosts = totalPosts,
                TotalRevenue = totalRevenue,
                TotalEscrowHolding = totalEscrowHolding
            };

            return ServiceResult<AdminDashboardDto>.SuccessResult(dashboard);
        }
    }
}
