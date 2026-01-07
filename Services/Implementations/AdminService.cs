using MentorEdu.DTOs.Category;
using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Report;
using MentorEdu.DTOs.User;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using MentorEdu.Services.Common;
using MentorEdu.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Services.Implementations
{
    public class AdminService : BaseService, IAdminService
    {
        public AdminService(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
        }

        #region User Management

        public async Task<ServiceResult<PagedResponse<UserBasicDto>>> GetUsersAsync(int pageNumber, int pageSize, UserRole? role = null, bool? isActive = null)
        {
            var query = _unitOfWork.Users.Query();

            if (role.HasValue)
                query = query.Where(u => u.Role == role.Value);

            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            var totalCount = await query.CountAsync();

            var users = await query
                .Include(u => u.UserProfile)
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = users.Select(u => new UserBasicDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.UserProfile?.FullName,
                AvatarUrl = u.UserProfile?.AvatarUrl,
                Role = u.Role,
                IsVerified = u.IsVerified,
                IsActive = u.IsActive
            });

            var response = PagedResponse<UserBasicDto>.Create(dtos, pageNumber, pageSize, totalCount);
            return ServiceResult<PagedResponse<UserBasicDto>>.SuccessResult(response);
        }

        public async Task<ServiceResult<UserDetailDto>> GetUserDetailAsync(long userId)
        {
            var user = await _unitOfWork.Users.GetByIdWithProfilesAsync(userId);
            
            if (user == null)
                return ServiceResult<UserDetailDto>.NotFoundResult("User not found");

            var dto = new UserDetailDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role,
                IsVerified = user.IsVerified,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                FullName = user.UserProfile?.FullName,
                AvatarUrl = user.UserProfile?.AvatarUrl,
                Phone = user.UserProfile?.Phone,
                Gender = user.UserProfile?.Gender,
                School = user.UserProfile?.School,
                Major = user.UserProfile?.Major,
                Bio = user.UserProfile?.Bio,
                City = user.UserProfile?.City,
                Country = user.UserProfile?.Country
            };

            if (user.Role == UserRole.Mentor && user.MentorProfile != null)
            {
                dto.MentorProfile = new MentorProfileDto
                {
                    Title = user.MentorProfile.Title,
                    HourlyRate = user.MentorProfile.HourlyRate,
                    PackagePrice = user.MentorProfile.PackagePrice,
                    ExperienceYears = user.MentorProfile.ExperienceYears,
                    Introduction = user.MentorProfile.Introduction,
                    AvailabilityNote = user.MentorProfile.AvailabilityNote,
                    RatingAvg = user.MentorProfile.RatingAvg,
                    RatingCount = user.MentorProfile.RatingCount
                };
            }

            return ServiceResult<UserDetailDto>.SuccessResult(dto);
        }

        public async Task<ServiceResult> ActivateUserAsync(long userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            
            if (user == null)
                return ServiceResult.NotFoundResult("User not found");

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Account activated successfully");
        }

        public async Task<ServiceResult> DeactivateUserAsync(long userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            
            if (user == null)
                return ServiceResult.NotFoundResult("User not found");

            if (user.Role == UserRole.Admin)
                return ServiceResult.FailResult("Cannot deactivate admin account");

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Account deactivated successfully");
        }

        public async Task<ServiceResult> BanUserAsync(long userId, string reason)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            
            if (user == null)
                return ServiceResult.NotFoundResult("User not found");

            if (user.Role == UserRole.Admin)
                return ServiceResult.FailResult("Cannot ban admin account");

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);

            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Account banned successfully");
        }

        public async Task<ServiceResult> VerifyUserAsync(long userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            
            if (user == null)
                return ServiceResult.NotFoundResult("User not found");

            user.IsVerified = true;
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Account verified successfully");
        }

        #endregion

        #region Category Management

        public async Task<ServiceResult<List<CategoryResponseDto>>> GetCategoriesAsync()
        {
            var categories = await _unitOfWork.Categories.GetAllWithHierarchyAsync();

            var dtos = categories.Select(c => MapToCategoryDto(c)).ToList();

            return ServiceResult<List<CategoryResponseDto>>.SuccessResult(dtos);
        }

        public async Task<ServiceResult<CategoryResponseDto>> GetCategoryByIdAsync(long categoryId)
        {
            var category = await _unitOfWork.Categories.GetWithChildrenAsync(categoryId);
            
            if (category == null)
                return ServiceResult<CategoryResponseDto>.NotFoundResult("Category not found");

            var dto = MapToCategoryDto(category);
            return ServiceResult<CategoryResponseDto>.SuccessResult(dto);
        }

        public async Task<ServiceResult<CategoryResponseDto>> CreateCategoryAsync(CreateCategoryRequest request)
        {
            // Check parent exists
            if (request.ParentId.HasValue)
            {
                var parent = await _unitOfWork.Categories.GetByIdAsync(request.ParentId.Value);
                if (parent == null)
                    return ServiceResult<CategoryResponseDto>.FailResult("Parent category does not exist");
            }

            var category = new Category
            {
                Name = request.Name,
                Description = request.Description,
                ParentId = request.ParentId
            };

            await _unitOfWork.Categories.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            var dto = new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ParentId = category.ParentId
            };

            return ServiceResult<CategoryResponseDto>.CreatedResult(dto, "Category created successfully");
        }

        public async Task<ServiceResult<CategoryResponseDto>> UpdateCategoryAsync(long categoryId, UpdateCategoryRequest request)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(categoryId);
            
            if (category == null)
                return ServiceResult<CategoryResponseDto>.NotFoundResult("Category not found");

            // Check circular reference
            if (request.ParentId.HasValue && request.ParentId.Value == categoryId)
                return ServiceResult<CategoryResponseDto>.FailResult("Category cannot be its own parent");

            category.Name = request.Name;
            category.Description = request.Description;
            category.ParentId = request.ParentId;

            _unitOfWork.Categories.Update(category);
            await _unitOfWork.SaveChangesAsync();

            var dto = new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ParentId = category.ParentId
            };

            return ServiceResult<CategoryResponseDto>.SuccessResult(dto, "Category updated successfully");
        }

        public async Task<ServiceResult> DeleteCategoryAsync(long categoryId)
        {
            var category = await _unitOfWork.Categories.GetWithChildrenAsync(categoryId);
            
            if (category == null)
                return ServiceResult.NotFoundResult("Category not found");

            if (category.Children?.Any() == true)
                return ServiceResult.FailResult("Cannot delete category with child categories");

            // Check if category is being used
            var requestsCount = await _unitOfWork.Requests.CountAsync(r => r.CategoryId == categoryId);
            if (requestsCount > 0)
                return ServiceResult.FailResult("Cannot delete category that is in use");

            _unitOfWork.Categories.Remove(category);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Category deleted successfully");
        }

        #endregion

        #region Report Management

        public async Task<ServiceResult<PagedResponse<ReportDto>>> GetReportsAsync(int pageNumber, int pageSize, string? status = null)
        {
            var query = _unitOfWork.Reports.Query();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(r => r.Status == status);

            var totalCount = await query.CountAsync();

            var reports = await query
                .Include(r => r.Reporter)
                    .ThenInclude(u => u.UserProfile)
                .Include(r => r.ReportedUser)
                    .ThenInclude(u => u.UserProfile)
                .Include(r => r.Order)
                .Include(r => r.Handler)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = reports.Select(r => new ReportDto
            {
                Id = r.Id,
                Reason = r.Reason,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                ReporterId = r.ReporterId,
                ReporterName = r.Reporter?.UserProfile?.FullName,
                ReportedUserId = r.ReportedUserId,
                ReportedUserName = r.ReportedUser?.UserProfile?.FullName,
                OrderId = r.OrderId,
                OrderTitle = r.Order?.Request?.Title,
                HandledBy = r.HandledBy,
                HandlerName = r.Handler?.UserProfile?.FullName
            });

            var response = PagedResponse<ReportDto>.Create(dtos, pageNumber, pageSize, totalCount);
            return ServiceResult<PagedResponse<ReportDto>>.SuccessResult(response);
        }

        public async Task<ServiceResult<ReportDetailDto>> GetReportDetailAsync(long reportId)
        {
            var report = await _unitOfWork.Reports.Query()
                .Include(r => r.Reporter)
                    .ThenInclude(u => u.UserProfile)
                .Include(r => r.ReportedUser)
                    .ThenInclude(u => u.UserProfile)
                .Include(r => r.Order)
                    .ThenInclude(o => o!.Request)
                .Include(r => r.Handler)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null)
                return ServiceResult<ReportDetailDto>.NotFoundResult("Report not found");

            // Get total reports for reported user
            var reportedUserReportCount = await _unitOfWork.Reports.CountAsync(r => r.ReportedUserId == report.ReportedUserId);

            var dto = new ReportDetailDto
            {
                Id = report.Id,
                Reason = report.Reason,
                Status = report.Status,
                CreatedAt = report.CreatedAt,
                UpdatedAt = report.UpdatedAt,
                Reporter = new ReportUserDto
                {
                    Id = report.ReporterId,
                    FullName = report.Reporter?.UserProfile?.FullName,
                    Email = report.Reporter?.Email,
                    AvatarUrl = report.Reporter?.UserProfile?.AvatarUrl
                },
                ReportedUser = new ReportUserDto
                {
                    Id = report.ReportedUserId,
                    FullName = report.ReportedUser?.UserProfile?.FullName,
                    Email = report.ReportedUser?.Email,
                    AvatarUrl = report.ReportedUser?.UserProfile?.AvatarUrl,
                    TotalReports = reportedUserReportCount
                },
                Order = report.Order != null ? new ReportOrderDto
                {
                    Id = report.Order.Id,
                    Title = report.Order.Request?.Title,
                    Status = report.Order.Status.ToString(),
                    TotalPrice = report.Order.TotalPrice
                } : null,
                HandlerName = report.Handler?.UserProfile?.FullName
            };

            return ServiceResult<ReportDetailDto>.SuccessResult(dto);
        }

        public async Task<ServiceResult> HandleReportAsync(long adminId, HandleReportDto request)
        {
            var report = await _unitOfWork.Reports.GetByIdAsync(request.ReportId);
            
            if (report == null)
                return ServiceResult.NotFoundResult("Report not found");

            report.Status = request.Status;
            report.HandledBy = adminId;
            report.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Reports.Update(report);

            // Ban user if required
            if (request.BanUser)
            {
                var reportedUser = await _unitOfWork.Users.GetByIdAsync(report.ReportedUserId);
                if (reportedUser != null && reportedUser.Role != UserRole.Admin)
                {
                    reportedUser.IsActive = false;
                    reportedUser.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Users.Update(reportedUser);
                }
            }

            // TODO: Send notification to reporter and reported user

            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Report handled successfully");
        }

        #endregion

        #region Dashboard

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
            var totalEscrowHolding = await _unitOfWork.Escrows.GetTotalHoldingAmountAsync();

            // Calculate total revenue from completed orders
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
                TotalRevenue = totalRevenue,
                TotalEscrowHolding = totalEscrowHolding
            };

            return ServiceResult<AdminDashboardDto>.SuccessResult(dashboard);
        }

        #endregion

        #region Private Methods

        private CategoryResponseDto MapToCategoryDto(Category category)
        {
            return new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ParentId = category.ParentId,
                ParentName = category.Parent?.Name,
                Children = category.Children?.Select(c => MapToCategoryDto(c)).ToList()
            };
        }

        #endregion
    }
}
