using MentorEdu.DTOs.Admin;
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
using System.Text.Json;

namespace MentorEdu.Services.Implementations
{
    public class AdminService : BaseService, IAdminService
    {
        private readonly IEmailService _emailService;
        private readonly IGeminiAiService _geminiAiService;
        private readonly ILogger<AdminService> _logger;

        public AdminService(IUnitOfWork unitOfWork, IEmailService emailService, IGeminiAiService geminiAiService, ILogger<AdminService> logger) : base(unitOfWork)
        {
            _emailService = emailService;
            _geminiAiService = geminiAiService;
            _logger = logger;
        }

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

        #endregion

        #region User Management

        public async Task<ServiceResult<PagedResponse<UserBasicDto>>> GetUsersAsync(int pageNumber, int pageSize, UserRole? role = null, bool? isActive = null, string? keyword = null)
        {
            var query = _unitOfWork.Users.Query();

            if (role.HasValue)
                query = query.Where(u => u.Role == role.Value);

            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(u => u.Email.Contains(keyword) || (u.UserProfile != null && u.UserProfile.FullName != null && u.UserProfile.FullName.Contains(keyword)));

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

        public async Task<ServiceResult<UserDetailDto>> AdminCreateUserAsync(AdminCreateUserDto request)
        {
            // Check email exists
            var existingUser = await _unitOfWork.Users.GetByEmailAsync(request.Email);
            if (existingUser != null)
                return ServiceResult<UserDetailDto>.FailResult("Email already exists");

            return await _unitOfWork.ExecuteWithStrategyAsync(async () =>
            {
                // Create user
                var user = new User
                {
                    Email = request.Email,
                    PasswordHash = HashPassword(request.Password),
                    Role = request.Role,
                    IsVerified = request.IsVerified,
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                // Create user profile
                var userProfile = new UserProfile
                {
                    UserId = user.Id,
                    FullName = request.FullName,
                    Phone = request.Phone,
                    Gender = request.Gender,
                    School = request.School,
                    Major = request.Major,
                    Bio = request.Bio,
                    City = request.City,
                    Country = request.Country
                };

                await _unitOfWork.UserProfiles.AddAsync(userProfile);

                // Create wallet
                var wallet = new Wallet
                {
                    UserId = user.Id,
                    Balance = 0,
                    LockedBalance = 0,
                    Currency = "VND",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Wallets.AddAsync(wallet);

                // If mentor, create mentor profile
                if (request.Role == UserRole.Mentor)
                {
                    var mentorProfile = new MentorProfile
                    {
                        UserId = user.Id,
                        Title = "New Mentor",
                        HourlyRate = 0,
                        PackagePrice = 0,
                        ExperienceYears = 0,
                        Introduction = "",
                        AvailabilityNote = "",
                        RatingAvg = 0,
                        RatingCount = 0,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.MentorProfiles.AddAsync(mentorProfile);
                }

                await _unitOfWork.SaveChangesAsync();

                // Return created user detail
                var dto = new UserDetailDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Role = user.Role,
                    IsVerified = user.IsVerified,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    FullName = userProfile.FullName,
                    Phone = userProfile.Phone,
                    Gender = userProfile.Gender,
                    School = userProfile.School,
                    Major = userProfile.Major,
                    Bio = userProfile.Bio,
                    City = userProfile.City,
                    Country = userProfile.Country
                };

                return ServiceResult<UserDetailDto>.CreatedResult(dto, "User created successfully");
            });
        }

        public async Task<ServiceResult<UserDetailDto>> AdminUpdateUserAsync(long userId, AdminUpdateUserDto request)
        {
            var user = await _unitOfWork.Users.Query()
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return ServiceResult<UserDetailDto>.NotFoundResult("User not found");

            if (user.Role == UserRole.Admin)
                return ServiceResult<UserDetailDto>.FailResult("Cannot update admin account");

            // Check email unique (if changed)
            if (user.Email != request.Email)
            {
                var existingUser = await _unitOfWork.Users.GetByEmailAsync(request.Email);
                if (existingUser != null && existingUser.Id != userId)
                    return ServiceResult<UserDetailDto>.FailResult("Email already exists");
            }

            // Update user
            user.Email = request.Email;
            user.IsVerified = request.IsVerified;
            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            // Update password if provided
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.PasswordHash = HashPassword(request.Password);
            }

            _unitOfWork.Users.Update(user);

            // Update user profile
            if (user.UserProfile != null)
            {
                user.UserProfile.FullName = request.FullName;
                user.UserProfile.Phone = request.Phone;
                user.UserProfile.Gender = request.Gender;
                user.UserProfile.School = request.School;
                user.UserProfile.Major = request.Major;
                user.UserProfile.Bio = request.Bio;
                user.UserProfile.City = request.City;
                user.UserProfile.Country = request.Country;

                _unitOfWork.UserProfiles.Update(user.UserProfile);
            }

            await _unitOfWork.SaveChangesAsync();

            // Return updated user detail
            var dto = new UserDetailDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role,
                IsVerified = user.IsVerified,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                FullName = user.UserProfile?.FullName,
                Phone = user.UserProfile?.Phone,
                Gender = user.UserProfile?.Gender,
                School = user.UserProfile?.School,
                Major = user.UserProfile?.Major,
                Bio = user.UserProfile?.Bio,
                City = user.UserProfile?.City,
                Country = user.UserProfile?.Country
            };

            return ServiceResult<UserDetailDto>.SuccessResult(dto, "User updated successfully");
        }

        public async Task<ServiceResult> AdminDeleteUserAsync(long userId)
        {
            var user = await _unitOfWork.Users.Query()
                .Include(u => u.UserProfile)
                .Include(u => u.MentorProfile)
                .Include(u => u.Wallet)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return ServiceResult.NotFoundResult("User not found");

            if (user.Role == UserRole.Admin)
                return ServiceResult.FailResult("Cannot delete admin account");

            // Check if user has active orders
            var hasActiveOrders = await _unitOfWork.Orders.Query()
                .AnyAsync(o => (o.StudentId == userId || o.MentorId == userId) && 
                               (o.Status == OrderStatus.InProgress || o.Status == OrderStatus.PendingPayment));

            if (hasActiveOrders)
                return ServiceResult.FailResult("Cannot delete user with active orders");

            return await _unitOfWork.ExecuteWithStrategyAsync(async () =>
            {
                // Delete related data (cascade)
                if (user.UserProfile != null)
                    _unitOfWork.UserProfiles.Remove(user.UserProfile);

                if (user.MentorProfile != null)
                    _unitOfWork.MentorProfiles.Remove(user.MentorProfile);

                if (user.Wallet != null)
                {
                    // Check wallet balance
                    if (user.Wallet.Balance > 0)
                        return ServiceResult.FailResult("Cannot delete user with positive wallet balance");

                    _unitOfWork.Wallets.Remove(user.Wallet);
                }

                // Delete user
                _unitOfWork.Users.Remove(user);

                await _unitOfWork.SaveChangesAsync();

                return ServiceResult.SuccessResult("User deleted successfully");
            });
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
            var category = await _unitOfWork.Categories.GetByIdAsync(categoryId);
            
            if (category == null)
                return ServiceResult<CategoryResponseDto>.NotFoundResult("Category not found");

            var dto = MapToCategoryDto(category);
            return ServiceResult<CategoryResponseDto>.SuccessResult(dto);
        }

        public async Task<ServiceResult<CategoryResponseDto>> CreateCategoryAsync(CreateCategoryRequest request)
        {
            var category = new Category
            {
                Name = request.Name,
                Description = request.Description
            };

            await _unitOfWork.Categories.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            var dto = new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            };

            return ServiceResult<CategoryResponseDto>.CreatedResult(dto, "Category created successfully");
        }

        public async Task<ServiceResult<CategoryResponseDto>> UpdateCategoryAsync(long categoryId, UpdateCategoryRequest request)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(categoryId);
            
            if (category == null)
                return ServiceResult<CategoryResponseDto>.NotFoundResult("Category not found");

            category.Name = request.Name;
            category.Description = request.Description;

            _unitOfWork.Categories.Update(category);
            await _unitOfWork.SaveChangesAsync();

            var dto = new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            };

            return ServiceResult<CategoryResponseDto>.SuccessResult(dto, "Category updated successfully");
        }

        public async Task<ServiceResult> DeleteCategoryAsync(long categoryId)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(categoryId);
            
            if (category == null)
                return ServiceResult.NotFoundResult("Category not found");

            var requestsCount = await _unitOfWork.Requests.CountAsync(r => r.CategoryId == categoryId);
            if (requestsCount > 0)
                return ServiceResult.FailResult("Cannot delete category that is in use");

            _unitOfWork.Categories.Remove(category);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Category deleted successfully");
        }

        #endregion

        #region Category-Hashtag Mapping

        public async Task<ServiceResult<List<CategoryHashtagMappingDto>>> GetCategoryHashtagMappingsAsync()
        {
            var mappings = await _unitOfWork.CategoryHashtags.GetAllWithDetailsAsync();
            
            var grouped = mappings
                .GroupBy(m => new { m.CategoryId, m.Category.Name })
                .Select(g => new CategoryHashtagMappingDto
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.Name,
                    Hashtags = g.Select(m => new DTOs.Category.CategoryHashtagItemDto
                    {
                        Id = m.HashtagId,
                        Name = m.Hashtag.Name
                    }).ToList()
                })
                .ToList();

            return ServiceResult<List<CategoryHashtagMappingDto>>.SuccessResult(grouped);
        }

        public async Task<ServiceResult<CategoryHashtagDetailDto>> GetHashtagsByCategoryAsync(long categoryId)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(categoryId);
            if (category == null)
                return ServiceResult<CategoryHashtagDetailDto>.NotFoundResult("Category not found");

            var hashtags = await _unitOfWork.CategoryHashtags.GetHashtagsByCategoryIdAsync(categoryId);
            
            var dto = new CategoryHashtagDetailDto
            {
                CategoryId = category.Id,
                CategoryName = category.Name,
                Hashtags = hashtags.Select(h => new DTOs.Category.CategoryHashtagItemDto
                {
                    Id = h.Id,
                    Name = h.Name
                }).ToList()
            };

            return ServiceResult<CategoryHashtagDetailDto>.SuccessResult(dto);
        }

        public async Task<ServiceResult> AddCategoryHashtagMappingAsync(AddCategoryHashtagDto request)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId);
            if (category == null)
                return ServiceResult.NotFoundResult("Category not found");

            var hashtag = await _unitOfWork.Hashtags.GetByIdAsync(request.HashtagId);
            if (hashtag == null)
                return ServiceResult.NotFoundResult("Hashtag not found");

            var exists = await _unitOfWork.CategoryHashtags.ExistsAsync(request.CategoryId, request.HashtagId);
            if (exists)
                return ServiceResult.FailResult("This mapping already exists");

            var mapping = new CategoryHashtag
            {
                CategoryId = request.CategoryId,
                HashtagId = request.HashtagId
            };

            await _unitOfWork.CategoryHashtags.AddAsync(mapping);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Category-Hashtag mapping created successfully");
        }

        public async Task<ServiceResult> RemoveCategoryHashtagMappingAsync(RemoveCategoryHashtagDto request)
        {
            var exists = await _unitOfWork.CategoryHashtags.ExistsAsync(request.CategoryId, request.HashtagId);
            if (!exists)
                return ServiceResult.NotFoundResult("Mapping not found");

            await _unitOfWork.CategoryHashtags.RemoveAsync(request.CategoryId, request.HashtagId);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Category-Hashtag mapping removed successfully");
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
                .Include(r => r.Reporter).ThenInclude(u => u.UserProfile)
                .Include(r => r.ReportedUser).ThenInclude(u => u.UserProfile)
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
                Details = r.Details,
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
                .Include(r => r.Reporter).ThenInclude(u => u.UserProfile)
                .Include(r => r.ReportedUser).ThenInclude(u => u.UserProfile)
                .Include(r => r.Order).ThenInclude(o => o!.Request)
                .Include(r => r.Handler)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null)
                return ServiceResult<ReportDetailDto>.NotFoundResult("Report not found");

            var reportedUserReportCount = await _unitOfWork.Reports.CountAsync(r => r.ReportedUserId == report.ReportedUserId);

            var dto = new ReportDetailDto
            {
                Id = report.Id,
                Reason = report.Reason,
                Details = report.Details,
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

            // If resolving a report that has a ReviewId, delete the review
            if (request.Status.Equals("Resolved", StringComparison.OrdinalIgnoreCase) && report.ReviewId.HasValue)
            {
                var reviewId = report.ReviewId.Value;
                var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
                if (review != null)
                {
                    // Clear review_id on all reports referencing this review to avoid FK constraint
                    var reportsWithReview = await _unitOfWork.Reports.Query()
                        .Where(r => r.ReviewId == reviewId)
                        .ToListAsync();
                    foreach (var rpt in reportsWithReview)
                    {
                        rpt.ReviewId = null;
                        _unitOfWork.Reports.Update(rpt);
                    }

                    // Recalculate mentor rating
                    var mentorProfile = await _unitOfWork.MentorProfiles.GetByUserIdAsync(review.ToUserId);
                    if (mentorProfile != null && mentorProfile.RatingCount > 0)
                    {
                        var remainingReviews = await _unitOfWork.Reviews.Query()
                            .Where(r => r.ToUserId == review.ToUserId && r.Id != review.Id)
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
                }
            }

            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.SuccessResult("Report handled successfully");
        }

        #endregion

        #region Requests Management

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

            var response = PagedResponse<AdminRequestListDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount);
            return ServiceResult<PagedResponse<AdminRequestListDto>>.SuccessResult(response);
        }

        public async Task<ServiceResult> AdminDeleteRequestAsync(long requestId, string reason)
        {
            var req = await _unitOfWork.Requests.Query()
                .Include(r => r.Orders)
                .Include(r => r.Proposals)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (req == null)
                return ServiceResult.NotFoundResult("Request not found");

            // Check if there are active orders
            if (req.Orders?.Any(o => o.Status == OrderStatus.InProgress || o.Status == OrderStatus.PendingPayment) == true)
                return ServiceResult.FailResult("Cannot delete request with active orders");

            // Delete related proposals first
            if (req.Proposals?.Any() == true)
            {
                foreach (var proposal in req.Proposals)
                {
                    _unitOfWork.Proposals.Remove(proposal);
                }
            }

            _unitOfWork.Requests.Remove(req);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Request deleted successfully");
        }

        #endregion

        #region Proposals Management

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

            var response = PagedResponse<AdminProposalListDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount);
            return ServiceResult<PagedResponse<AdminProposalListDto>>.SuccessResult(response);
        }

        public async Task<ServiceResult> AdminDeleteProposalAsync(long proposalId, string reason)
        {
            var proposal = await _unitOfWork.Proposals.Query()
                .Include(p => p.Orders)
                .FirstOrDefaultAsync(p => p.Id == proposalId);

            if (proposal == null)
                return ServiceResult.NotFoundResult("Proposal not found");

            if (proposal.Orders?.Any(o => o.Status == OrderStatus.InProgress || o.Status == OrderStatus.PendingPayment) == true)
                return ServiceResult.FailResult("Cannot delete proposal with active orders");

            _unitOfWork.Proposals.Remove(proposal);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Proposal deleted successfully");
        }

        #endregion

        #region Orders Management

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

            var dtos = orders.Select(o => new AdminOrderListDto
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
            });

            var response = PagedResponse<AdminOrderListDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount);
            return ServiceResult<PagedResponse<AdminOrderListDto>>.SuccessResult(response);
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

            var dto = new AdminOrderListDto
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
                CancelledAt = order.CancelledAt,
                CreatedAt = order.CreatedAt,
                StudentId = order.StudentId,
                StudentName = order.Student?.UserProfile?.FullName,
                StudentEmail = order.Student?.Email,
                MentorId = order.MentorId,
                MentorName = order.Mentor?.UserProfile?.FullName,
                MentorEmail = order.Mentor?.Email,
                HasEscrow = order.Escrow != null,
                EscrowAmount = order.Escrow?.Amount,
                EscrowStatus = order.Escrow?.Status.ToString(),
                ReportCount = order.Reports?.Count ?? 0
            };

            return ServiceResult<AdminOrderListDto>.SuccessResult(dto);
        }

        public async Task<ServiceResult> AdminUpdateOrderStatusAsync(long orderId, long adminId, AdminUpdateOrderStatusDto request)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);

            if (order == null)
                return ServiceResult.NotFoundResult("Order not found");

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
                ChangedBy = adminId,
                Note = $"[Admin Action] {request.Note}",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.OrderDetails.AddAsync(orderDetail);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Order status updated successfully");
        }

        public async Task<ServiceResult> AdminRefundOrderAsync(long orderId, long adminId, AdminRefundOrderDto request)
        {
            var order = await _unitOfWork.Orders.Query()
                .Include(o => o.Escrow)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return ServiceResult.NotFoundResult("Order not found");

            if (order.Escrow == null || order.Escrow.Status != EscrowStatus.Holding)
                return ServiceResult.FailResult("No escrow to refund");

            return await _unitOfWork.ExecuteWithStrategyAsync(async () =>
            {
                var refundAmount = request.FullRefund ? order.Escrow.Amount : request.Amount;

                if (refundAmount > order.Escrow.Amount)
                    return ServiceResult.FailResult("Refund amount exceeds escrow amount");

                // Update escrow
                order.Escrow.Status = EscrowStatus.Refunded;
                order.Escrow.RefundedAt = DateTime.UtcNow;

                // Update order status
                order.Status = OrderStatus.Cancelled;
                order.CancelledAt = DateTime.UtcNow;
                order.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Orders.Update(order);

                // Refund to student wallet
                var studentWallet = await _unitOfWork.Wallets.GetByUserIdAsync(order.StudentId);
                if (studentWallet != null)
                {
                    studentWallet.Balance += refundAmount;
                    studentWallet.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Wallets.Update(studentWallet);

                    // Create refund transaction
                    var transaction = new Transaction
                    {
                        WalletId = studentWallet.Id,
                        OrderId = orderId,
                        Type = TransactionType.EscrowRefund,
                        Amount = refundAmount,
                        Status = TransactionStatus.Success,
                        Description = $"[Admin Refund] {request.Reason}",
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.Transactions.AddAsync(transaction);
                }

                // Log status change
                var orderDetail = new OrderDetail
                {
                    OrderId = orderId,
                    OldStatus = order.Status.ToString(),
                    NewStatus = OrderStatus.Cancelled.ToString(),
                    ChangedBy = adminId,
                    Note = $"[Admin Refund] Amount: {refundAmount:N0} VND. Reason: {request.Reason}",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.OrderDetails.AddAsync(orderDetail);
                await _unitOfWork.SaveChangesAsync();

                return ServiceResult.SuccessResult($"Refund of {refundAmount:N0} VND processed successfully");
            });
        }

        #endregion

        #region Community Posts Management

        public async Task<ServiceResult<PagedResponse<AdminPostListDto>>> GetAllPostsAsync(AdminSearchPostDto request)
        {
            var query = _unitOfWork.CommunityPosts.Query();

            if (!string.IsNullOrWhiteSpace(request.Keyword))
                query = query.Where(p => p.Title!.Contains(request.Keyword) || p.Content!.Contains(request.Keyword));

            if (request.AuthorId.HasValue)
                query = query.Where(p => p.AuthorId == request.AuthorId);

            if (request.FromDate.HasValue)
                query = query.Where(p => p.CreatedAt >= request.FromDate);

            if (request.ToDate.HasValue)
                query = query.Where(p => p.CreatedAt <= request.ToDate);

            if (request.IsHidden.HasValue)
                query = query.Where(p => p.IsHidden == request.IsHidden.Value);

            var totalCount = await query.CountAsync();

            var posts = await query
                .Include(p => p.Author).ThenInclude(a => a.UserProfile)
                .Include(p => p.Comments)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var dtos = posts.Select(p => new AdminPostListDto
            {
                Id = p.Id,
                Title = p.Title,
                ContentPreview = p.Content?.Length > 200 ? p.Content.Substring(0, 200) + "..." : p.Content,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                AuthorId = p.AuthorId,
                AuthorName = p.Author?.UserProfile?.FullName,
                AuthorEmail = p.Author?.Email,
                AuthorAvatar = p.Author?.UserProfile?.AvatarUrl,
                CommentCount = p.Comments?.Count ?? 0,
                ReportCount = 0,
                IsHidden = p.IsHidden
            });

            var response = PagedResponse<AdminPostListDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount);
            return ServiceResult<PagedResponse<AdminPostListDto>>.SuccessResult(response);
        }

        public async Task<ServiceResult> AdminDeletePostAsync(long postId, string reason)
        {
            var post = await _unitOfWork.CommunityPosts.Query()
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
                return ServiceResult.NotFoundResult("Post not found");

            // Delete related comments first
            if (post.Comments?.Any() == true)
            {
                foreach (var comment in post.Comments)
                {
                    _unitOfWork.CommunityComments.Remove(comment);
                }
            }

            _unitOfWork.CommunityPosts.Remove(post);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Post deleted successfully");
        }

        public async Task<ServiceResult> AdminHidePostAsync(long postId, string reason)
        {
            var post = await _unitOfWork.CommunityPosts.GetByIdAsync(postId);

            if (post == null)
                return ServiceResult.NotFoundResult("Post not found");

            post.IsHidden = true;
            post.HiddenReason = reason;
            post.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.CommunityPosts.Update(post);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Post hidden successfully");
        }

        public async Task<ServiceResult> AdminUnhidePostAsync(long postId)
        {
            var post = await _unitOfWork.CommunityPosts.GetByIdAsync(postId);

            if (post == null)
                return ServiceResult.NotFoundResult("Post not found");

            post.IsHidden = false;
            post.HiddenReason = null;
            post.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.CommunityPosts.Update(post);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Post unhidden successfully");
        }

        #endregion

        #region Community Comments Management

        public async Task<ServiceResult<PagedResponse<AdminCommentListDto>>> GetAllCommentsAsync(AdminSearchCommentDto request)
        {
            var query = _unitOfWork.CommunityComments.Query();

            if (request.PostId.HasValue)
                query = query.Where(c => c.PostId == request.PostId);

            if (request.AuthorId.HasValue)
                query = query.Where(c => c.AuthorId == request.AuthorId);

            if (!string.IsNullOrWhiteSpace(request.Keyword))
                query = query.Where(c => c.Content!.Contains(request.Keyword));

            if (request.FromDate.HasValue)
                query = query.Where(c => c.CreatedAt >= request.FromDate);

            if (request.ToDate.HasValue)
                query = query.Where(c => c.CreatedAt <= request.ToDate);

            var totalCount = await query.CountAsync();

            var comments = await query
                .Include(c => c.Author).ThenInclude(a => a.UserProfile)
                .Include(c => c.Post)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var dtos = comments.Select(c => new AdminCommentListDto
            {
                Id = c.Id,
                PostId = c.PostId,
                PostTitle = c.Post?.Title,
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                AuthorId = c.AuthorId,
                AuthorName = c.Author?.UserProfile?.FullName,
                AuthorEmail = c.Author?.Email
            });

            var response = PagedResponse<AdminCommentListDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount);
            return ServiceResult<PagedResponse<AdminCommentListDto>>.SuccessResult(response);
        }

        public async Task<ServiceResult> AdminDeleteCommentAsync(long commentId, string reason)
        {
            var comment = await _unitOfWork.CommunityComments.GetByIdAsync(commentId);

            if (comment == null)
                return ServiceResult.NotFoundResult("Comment not found");

            _unitOfWork.CommunityComments.Remove(comment);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Comment deleted successfully");
        }

        #endregion

        #region Transactions Management

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

            var response = PagedResponse<AdminTransactionListDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount);
            return ServiceResult<PagedResponse<AdminTransactionListDto>>.SuccessResult(response);
        }

        public async Task<ServiceResult> AdminVerifyTransactionAsync(long transactionId, long adminId, bool approve, string? note)
        {
            var transaction = await _unitOfWork.Transactions.GetByIdAsync(transactionId);

            if (transaction == null)
                return ServiceResult.NotFoundResult("Transaction not found");

            if (transaction.Status != TransactionStatus.Pending)
                return ServiceResult.FailResult("Only pending transactions can be verified");

            transaction.Status = approve ? TransactionStatus.Success : TransactionStatus.Failed;
            transaction.Description = $"{transaction.Description} | [Admin: {(approve ? "Approved" : "Rejected")}] {note}";

            _unitOfWork.Transactions.Update(transaction);

            // If approved and it's a topup, add balance
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

        #endregion

        #region Wallets Management

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

            var response = PagedResponse<AdminWalletListDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount);
            return ServiceResult<PagedResponse<AdminWalletListDto>>.SuccessResult(response);
        }

        public async Task<ServiceResult<AdminWalletListDto>> GetWalletDetailForAdminAsync(long walletId)
        {
            var wallet = await _unitOfWork.Wallets.Query()
                .Include(w => w.User).ThenInclude(u => u.UserProfile)
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.Id == walletId);

            if (wallet == null)
                return ServiceResult<AdminWalletListDto>.NotFoundResult("Wallet not found");

            var dto = new AdminWalletListDto
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
            };

            return ServiceResult<AdminWalletListDto>.SuccessResult(dto);
        }

        #endregion

        #region Reviews Management

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

            var response = PagedResponse<AdminReviewListDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount);
            return ServiceResult<PagedResponse<AdminReviewListDto>>.SuccessResult(response);
        }

        public async Task<ServiceResult> AdminDeleteReviewAsync(long reviewId, string reason)
        {
            var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);

            if (review == null)
                return ServiceResult.NotFoundResult("Review not found");

            // Update mentor rating if needed
            var mentorProfile = await _unitOfWork.MentorProfiles.GetByUserIdAsync(review.ToUserId);
            if (mentorProfile != null && mentorProfile.RatingCount > 0)
            {
                // Recalculate rating after removing this review
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

        #endregion

        #region Hashtags Management

        public async Task<ServiceResult<PagedResponse<AdminHashtagDto>>> GetAllHashtagsAsync(AdminSearchHashtagDto request)
        {
            var query = _unitOfWork.Hashtags.Query();

            if (!string.IsNullOrWhiteSpace(request.Keyword))
                query = query.Where(h => h.Name.Contains(request.Keyword));

            var totalCount = await query.CountAsync();

            var hashtags = await query
                .Include(h => h.MentorHashtags)
                .Include(h => h.CategoryHashtags)
                .OrderBy(h => h.Name)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var dtos = hashtags.Select(h => new AdminHashtagDto
            {
                Id = h.Id,
                Name = h.Name,
                MentorCount = h.MentorHashtags?.Count ?? 0,
                CategoryCount = h.CategoryHashtags?.Count ?? 0
            });

            var response = PagedResponse<AdminHashtagDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount);
            return ServiceResult<PagedResponse<AdminHashtagDto>>.SuccessResult(response);
        }

        public async Task<ServiceResult<List<HashtagDto>>> GetHashtagsAsync()
        {
            var hashtags = await _unitOfWork.Hashtags.Query()
                .OrderBy(h => h.Name)
                .ToListAsync();

            var dtos = hashtags.Select(h => new HashtagDto
            {
                Id = h.Id,
                Name = h.Name
            }).ToList();

            return ServiceResult<List<HashtagDto>>.SuccessResult(dtos);
        }

        public async Task<ServiceResult<AdminHashtagDto>> CreateHashtagAsync(CreateHashtagDto request)
        {
            // Check if exists
            var existing = await _unitOfWork.Hashtags.GetByNameAsync(request.Name);
            if (existing != null)
                return ServiceResult<AdminHashtagDto>.FailResult("Hashtag already exists");

            var hashtag = new Hashtag
            {
                Name = request.Name
            };

            await _unitOfWork.Hashtags.AddAsync(hashtag);
            await _unitOfWork.SaveChangesAsync();

            var dto = new AdminHashtagDto
            {
                Id = hashtag.Id,
                Name = hashtag.Name,
                MentorCount = 0,
                CategoryCount = 0
            };

            return ServiceResult<AdminHashtagDto>.CreatedResult(dto, "Hashtag created successfully");
        }

        public async Task<ServiceResult> DeleteHashtagAsync(long hashtagId)
        {
            var hashtag = await _unitOfWork.Hashtags.Query()
                .Include(h => h.MentorHashtags)
                .FirstOrDefaultAsync(h => h.Id == hashtagId);

            if (hashtag == null)
                return ServiceResult.NotFoundResult("Hashtag not found");

            if (hashtag.MentorHashtags?.Any() == true)
                return ServiceResult.FailResult("Cannot delete hashtag that is in use");

            _unitOfWork.Hashtags.Remove(hashtag);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Hashtag deleted successfully");
        }

        #endregion

        #region Mentor Applications Management

        public async Task<ServiceResult<PagedResponse<MentorApplicationDto>>> GetMentorApplicationsAsync(int pageNumber = 1, int pageSize = 10, MentorApprovalStatus? status = null)
        {
            var query = _unitOfWork.MentorProfiles.Query();

            // Filter by status if provided
            // If status is null, get ALL applications (not just Pending)
            if (status.HasValue)
                query = query.Where(mp => mp.ApprovalStatus == status);

            var totalCount = await query.CountAsync();

            var applications = await query
                .Include(mp => mp.User).ThenInclude(u => u.UserProfile)
                .Include(mp => mp.User).ThenInclude(u => u.MentorCategories).ThenInclude(mc => mc.Category)
                .Include(mp => mp.User).ThenInclude(u => u.MentorHashtags).ThenInclude(mh => mh.Hashtag)
                .Include(mp => mp.Reviewer).ThenInclude(r => r!.UserProfile)
                .OrderByDescending(mp => mp.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = applications.Select(mp => new MentorApplicationDto
            {
                UserId = mp.UserId,
                Email = mp.User?.Email ?? "",
                FullName = mp.User?.UserProfile?.FullName ?? "",
                AvatarUrl = mp.User?.UserProfile?.AvatarUrl,
                Specialization = mp.Specialization,
                ExperienceYears = mp.ExperienceYears,
                Introduction = mp.Introduction,
                PortfolioUrl = mp.PortfolioUrl,
                CertificationUrls = mp.CertificationUrls,
                ApplicationNote = mp.ApplicationNote,
                Categories = mp.User?.MentorCategories?.Select(mc => new CategorySimpleDto
                {
                    Id = mc.CategoryId,
                    Name = mc.Category?.Name ?? ""
                }).ToList() ?? new List<CategorySimpleDto>(),
                Hashtags = mp.User?.MentorHashtags?.Select(mh => new HashtagSimpleDto
                {
                    Id = mh.HashtagId,
                    Name = mh.Hashtag?.Name ?? ""
                }).ToList() ?? new List<HashtagSimpleDto>(),
                ApprovalStatus = mp.ApprovalStatus,
                CreatedAt = mp.CreatedAt,
                ReviewedAt = mp.ReviewedAt,
                ReviewedByName = mp.Reviewer?.UserProfile?.FullName,
                RejectionReason = mp.RejectionReason
            });

            var response = PagedResponse<MentorApplicationDto>.Create(dtos, pageNumber, pageSize, totalCount);
            return ServiceResult<PagedResponse<MentorApplicationDto>>.SuccessResult(response);
        }

        public async Task<ServiceResult<MentorApplicationDto>> GetMentorApplicationDetailAsync(long userId)
        {
            var mentorProfile = await _unitOfWork.MentorProfiles.Query()
                .Include(mp => mp.User).ThenInclude(u => u.UserProfile)
                .Include(mp => mp.User).ThenInclude(u => u.MentorCategories).ThenInclude(mc => mc.Category)
                .Include(mp => mp.User).ThenInclude(u => u.MentorHashtags).ThenInclude(mh => mh.Hashtag)
                .Include(mp => mp.Reviewer).ThenInclude(r => r!.UserProfile)
                .FirstOrDefaultAsync(mp => mp.UserId == userId);

            if (mentorProfile == null)
                return ServiceResult<MentorApplicationDto>.NotFoundResult("Mentor application not found");

            var dto = new MentorApplicationDto
            {
                UserId = mentorProfile.UserId,
                Email = mentorProfile.User?.Email ?? "",
                FullName = mentorProfile.User?.UserProfile?.FullName ?? "",
                AvatarUrl = mentorProfile.User?.UserProfile?.AvatarUrl,
                Specialization = mentorProfile.Specialization,
                ExperienceYears = mentorProfile.ExperienceYears,
                Introduction = mentorProfile.Introduction,
                PortfolioUrl = mentorProfile.PortfolioUrl,
                CertificationUrls = mentorProfile.CertificationUrls,
                ApplicationNote = mentorProfile.ApplicationNote,
                Categories = mentorProfile.User?.MentorCategories?.Select(mc => new CategorySimpleDto
                {
                    Id = mc.CategoryId,
                    Name = mc.Category?.Name ?? ""
                }).ToList() ?? new List<CategorySimpleDto>(),
                Hashtags = mentorProfile.User?.MentorHashtags?.Select(mh => new HashtagSimpleDto
                {
                    Id = mh.HashtagId,
                    Name = mh.Hashtag?.Name ?? ""
                }).ToList() ?? new List<HashtagSimpleDto>(),
                ApprovalStatus = mentorProfile.ApprovalStatus,
                CreatedAt = mentorProfile.CreatedAt,
                ReviewedAt = mentorProfile.ReviewedAt,
                ReviewedByName = mentorProfile.Reviewer?.UserProfile?.FullName,
                RejectionReason = mentorProfile.RejectionReason
            };

            // Get the latest AI analysis from application history
            var latestHistory = await _unitOfWork.MentorApplicationHistories.Query()
                .Where(h => h.UserId == userId && h.AiConfidenceScore != null)
                .OrderByDescending(h => h.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestHistory != null)
            {
                dto.AiConfidenceScore = latestHistory.AiConfidenceScore;
                dto.AiModelVersion = latestHistory.AiModelVersion;
                dto.IsAiReviewed = true;

                // Parse ApplicationSnapshot to extract AI analysis details
                if (!string.IsNullOrEmpty(latestHistory.ApplicationSnapshot))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(latestHistory.ApplicationSnapshot);
                        if (doc.RootElement.TryGetProperty("AiAnalysis", out var aiAnalysis) && 
                            aiAnalysis.ValueKind != JsonValueKind.Null)
                        {
                            if (aiAnalysis.TryGetProperty("AiRecommendedAction", out var actionProp))
                                dto.AiRecommendedAction = actionProp.GetString();
                            
                            if (aiAnalysis.TryGetProperty("AiReasoning", out var reasoningProp))
                                dto.AiReasoning = reasoningProp.GetString();
                            
                            if (aiAnalysis.TryGetProperty("AiKeyPoints", out var keyPointsProp))
                            {
                                dto.AiKeyPoints = new List<string>();
                                foreach (var point in keyPointsProp.EnumerateArray())
                                {
                                    var value = point.GetString();
                                    if (!string.IsNullOrWhiteSpace(value))
                                        dto.AiKeyPoints.Add(value);
                                }
                            }
                            
                            if (aiAnalysis.TryGetProperty("AiRedFlags", out var redFlagsProp))
                            {
                                dto.AiRedFlags = new List<string>();
                                foreach (var flag in redFlagsProp.EnumerateArray())
                                {
                                    var value = flag.GetString();
                                    if (!string.IsNullOrWhiteSpace(value))
                                        dto.AiRedFlags.Add(value);
                                }
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Ignore JSON parsing errors - AI fields will remain null
                    }
                }
            }

            return ServiceResult<MentorApplicationDto>.SuccessResult(dto);
        }

        public async Task<ServiceResult> ApproveMentorApplicationAsync(long userId, long adminId)
        {
            var mentorProfile = await _unitOfWork.MentorProfiles.GetByUserIdAsync(userId);

            if (mentorProfile == null)
                return ServiceResult.NotFoundResult("Mentor application not found");

            if (mentorProfile.ApprovalStatus == MentorApprovalStatus.Approved)
                return ServiceResult.FailResult("This application has already been approved");

            var user = await _unitOfWork.Users.GetByIdWithProfilesAsync(userId);
            if (user == null)
                return ServiceResult.NotFoundResult("User not found");

            // Get submission count
            var submissionCount = await _unitOfWork.MentorApplicationHistories.GetSubmissionCountAsync(userId);

            return await _unitOfWork.ExecuteWithStrategyAsync(async () =>
            {
                // Update mentor profile status (DO NOT change user role - user will confirm later)
                mentorProfile.ApprovalStatus = MentorApprovalStatus.Approved;
                mentorProfile.ReviewedAt = DateTime.UtcNow;
                mentorProfile.ReviewedBy = adminId;
                mentorProfile.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.MentorProfiles.Update(mentorProfile);

                // Record history for audit trail and ML training
                var applicationSnapshot = JsonSerializer.Serialize(new
                {
                    mentorProfile.Specialization,
                    mentorProfile.ExperienceYears,
                    mentorProfile.Introduction,
                    mentorProfile.PortfolioUrl,
                    mentorProfile.ApplicationNote,
                    mentorProfile.Title
                });

                var history = new MentorApplicationHistory
                {
                    UserId = userId,
                    Action = ApplicationAction.Approved,
                    ApplicationSnapshot = applicationSnapshot,
                    CertificationUrls = mentorProfile.CertificationUrls,
                    SubmissionCount = submissionCount > 0 ? submissionCount : 1,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = adminId
                };
                await _unitOfWork.MentorApplicationHistories.AddAsync(history);

                // Create notification for user
                var notification = new Notification
                {
                    UserId = userId,
                    Type = NotificationType.System,
                    Title = "Mentor Application Approved",
                    Body = "Congratulations! Your mentor application has been approved. Please go to your Profile to confirm switching to Mentor role.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Notifications.AddAsync(notification);

                await _unitOfWork.SaveChangesAsync();

                // Send email notification to user
                var fullName = user.UserProfile?.FullName ?? user.Email;
                try
                {
                    await _emailService.SendMentorApprovalNotificationAsync(user.Email, fullName);
                }
                catch
                {
                    // Log but don't fail the approval if email fails
                }

                return ServiceResult.SuccessResult("Mentor application approved successfully. User needs to confirm the role switch.");
            });
        }


        public async Task<ServiceResult> RejectMentorApplicationAsync(long userId, long adminId, string reason)
        {
            var mentorProfile = await _unitOfWork.MentorProfiles.GetByUserIdAsync(userId);

            if (mentorProfile == null)
                return ServiceResult.NotFoundResult("Mentor application not found");

            if (mentorProfile.ApprovalStatus != MentorApprovalStatus.Pending)
                return ServiceResult.FailResult("Only pending applications can be rejected");

            // Get user info with profile for email (using same method as ApproveMentorApplicationAsync)
            var user = await _unitOfWork.Users.GetByIdWithProfilesAsync(userId);
            if (user == null)
                return ServiceResult.NotFoundResult("User not found");

            var fullName = user.UserProfile?.FullName ?? user.Email.Split('@')[0];

            // Get submission count
            var submissionCount = await _unitOfWork.MentorApplicationHistories.GetSubmissionCountAsync(userId);

            // Update mentor profile status
            mentorProfile.ApprovalStatus = MentorApprovalStatus.Rejected;
            mentorProfile.ReviewedAt = DateTime.UtcNow;
            mentorProfile.ReviewedBy = adminId;
            mentorProfile.RejectionReason = reason;
            mentorProfile.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.MentorProfiles.Update(mentorProfile);

            // Record history for audit trail and ML training
            var applicationSnapshot = JsonSerializer.Serialize(new
            {
                mentorProfile.Specialization,
                mentorProfile.ExperienceYears,
                mentorProfile.Introduction,
                mentorProfile.PortfolioUrl,
                mentorProfile.ApplicationNote,
                mentorProfile.Title
            });

            var history = new MentorApplicationHistory
            {
                UserId = userId,
                Action = ApplicationAction.Rejected,
                ApplicationSnapshot = applicationSnapshot,
                CertificationUrls = mentorProfile.CertificationUrls,
                Reason = reason,
                SubmissionCount = submissionCount > 0 ? submissionCount : 1,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = adminId
            };
            await _unitOfWork.MentorApplicationHistories.AddAsync(history);

            // Create notification for user
            var notification = new Notification
            {
                UserId = userId,
                Type = NotificationType.System,
                Title = "Mentor Application Rejected",
                Body = $"Your mentor application has been rejected. Reason: {reason}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Notifications.AddAsync(notification);

            await _unitOfWork.SaveChangesAsync();

            // Send email notification to user
            try
            {
                await _emailService.SendMentorRejectionNotificationAsync(user.Email, fullName, reason);
            }
            catch (Exception)
            {
                // Log error but don't fail the operation
                // Email sending failure shouldn't block the rejection
            }

            return ServiceResult.SuccessResult("Mentor application rejected");
        }

        public async Task<ServiceResult<MentorApplicationDto>> ReEvaluateMentorApplicationAsync(long userId, long adminId)
        {
            var mentorProfile = await _unitOfWork.MentorProfiles.Query()
                .Include(mp => mp.User).ThenInclude(u => u.UserProfile)
                .Include(mp => mp.User).ThenInclude(u => u.MentorCategories).ThenInclude(mc => mc.Category)
                .Include(mp => mp.User).ThenInclude(u => u.MentorHashtags).ThenInclude(mh => mh.Hashtag)
                .FirstOrDefaultAsync(mp => mp.UserId == userId);

            if (mentorProfile == null)
                return ServiceResult<MentorApplicationDto>.NotFoundResult("Mentor application not found");

            if (!_geminiAiService.IsConfigured())
                return ServiceResult<MentorApplicationDto>.FailResult("AI service is not configured");

            // Reconstruct the request from existing profile data
            var request = new ApplyAsMentorRequest
            {
                Specialization = mentorProfile.Specialization ?? "",
                ExperienceYears = mentorProfile.ExperienceYears ?? 0,
                Introduction = mentorProfile.Introduction ?? "",
                PortfolioUrl = mentorProfile.PortfolioUrl ?? "",
                ApplicationNote = mentorProfile.ApplicationNote,
                CertificationUrls = mentorProfile.CertificationUrls ?? "",
                CategoryIds = mentorProfile.User?.MentorCategories?.Select(mc => mc.CategoryId).ToList() ?? new List<long>(),
                HashtagIds = mentorProfile.User?.MentorHashtags?.Select(mh => mh.HashtagId).ToList() ?? new List<long>()
            };

            // Run AI analysis
            GeminiAnalysisResult aiResult;
            try
            {
                _logger.LogInformation("Admin {AdminId} triggered AI re-evaluation for user {UserId}", adminId, userId);
                aiResult = await _geminiAiService.AnalyzeMentorApplicationAsync(userId, request, request.CertificationUrls);
                _logger.LogInformation("AI re-evaluation for user {UserId}: Score={Score}, Action={Action}",
                    userId, aiResult.ConfidenceScore, aiResult.RecommendedAction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI re-evaluation failed for user {UserId}", userId);
                return ServiceResult<MentorApplicationDto>.FailResult($"AI analysis failed: {ex.Message}");
            }

            // Get submission count
            var submissionCount = await _unitOfWork.MentorApplicationHistories.GetSubmissionCountAsync(userId);

            // Apply AI result
            string resultMessage;
            ApplicationAction historyAction = ApplicationAction.AiReEvaluated;
            string? historyReason;

            if (aiResult.RecommendedAction == "AUTO_APPROVE")
            {
                mentorProfile.ApprovalStatus = MentorApprovalStatus.Approved;
                mentorProfile.ReviewedAt = DateTime.UtcNow;
                mentorProfile.ReviewedBy = adminId;
                mentorProfile.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.MentorProfiles.Update(mentorProfile);

                historyReason = $"AI re-evaluation: Auto-approved (Score: {aiResult.ConfidenceScore}%). {aiResult.Reasoning}";
                resultMessage = $"AI approved the application (Score: {aiResult.ConfidenceScore}%).";

                // Create approval notification
                var notification = new Notification
                {
                    UserId = userId,
                    Type = NotificationType.System,
                    Title = "Mentor Application Approved",
                    Body = $"Your mentor application has been approved by AI review (Score: {aiResult.ConfidenceScore}%). Go to Profile to confirm switching to Mentor role.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Notifications.AddAsync(notification);
            }
            else if (aiResult.RecommendedAction == "AUTO_REJECT")
            {
                var redFlagsText = aiResult.RedFlags.Any()
                    ? string.Join("; ", aiResult.RedFlags)
                    : "Application did not meet minimum requirements";

                mentorProfile.ApprovalStatus = MentorApprovalStatus.Rejected;
                mentorProfile.ReviewedAt = DateTime.UtcNow;
                mentorProfile.ReviewedBy = adminId;
                mentorProfile.RejectionReason = $"AI review concerns: {redFlagsText}. You may reapply with improved credentials.";
                mentorProfile.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.MentorProfiles.Update(mentorProfile);

                historyReason = $"AI re-evaluation: Auto-rejected (Score: {aiResult.ConfidenceScore}%). Concerns: {redFlagsText}";
                resultMessage = $"AI rejected the application (Score: {aiResult.ConfidenceScore}%). Concerns: {redFlagsText}";

                // Create rejection notification
                var notification = new Notification
                {
                    UserId = userId,
                    Type = NotificationType.System,
                    Title = "Mentor Application Rejected",
                    Body = $"Your mentor application was rejected by AI review. Concerns: {redFlagsText}. You may reapply.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Notifications.AddAsync(notification);
            }
            else
            {
                // Still pending - manual review needed
                historyReason = $"AI re-evaluation: Still pending manual review (Score: {aiResult.ConfidenceScore}%). {aiResult.Reasoning}";
                resultMessage = $"AI scored the application at {aiResult.ConfidenceScore}%. Still requires manual review.";
            }

            // Save AI analysis data in history
            var aiAnalysisData = new
            {
                AiConfidenceScore = aiResult.ConfidenceScore,
                AiRecommendedAction = aiResult.RecommendedAction,
                AiReasoning = aiResult.Reasoning,
                AiKeyPoints = aiResult.KeyPoints,
                AiRedFlags = aiResult.RedFlags,
                AiModelVersion = aiResult.ModelVersion
            };

            var applicationSnapshot = JsonSerializer.Serialize(new
            {
                mentorProfile.Specialization,
                mentorProfile.ExperienceYears,
                mentorProfile.Introduction,
                mentorProfile.PortfolioUrl,
                mentorProfile.ApplicationNote,
                AiAnalysis = aiAnalysisData
            });

            var history = new MentorApplicationHistory
            {
                UserId = userId,
                Action = historyAction,
                ApplicationSnapshot = applicationSnapshot,
                CertificationUrls = mentorProfile.CertificationUrls,
                Reason = historyReason,
                AiConfidenceScore = aiResult.ConfidenceScore,
                AiModelVersion = aiResult.ModelVersion,
                SubmissionCount = submissionCount > 0 ? submissionCount : 1,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = adminId
            };
            await _unitOfWork.MentorApplicationHistories.AddAsync(history);

            await _unitOfWork.SaveChangesAsync();

            // Send email notification based on AI decision
            if (mentorProfile.User != null)
            {
                var fullName = mentorProfile.User.UserProfile?.FullName ?? mentorProfile.User.Email;
                try
                {
                    if (aiResult.RecommendedAction == "AUTO_APPROVE")
                    {
                        await _emailService.SendMentorApprovalNotificationAsync(mentorProfile.User.Email, fullName);
                        _logger.LogInformation("Sent approval email after re-evaluation for user {UserId}", userId);
                    }
                    else if (aiResult.RecommendedAction == "AUTO_REJECT")
                    {
                        var rejectReason = aiResult.RedFlags.Any()
                            ? string.Join("; ", aiResult.RedFlags)
                            : "Application did not meet minimum requirements";
                        await _emailService.SendMentorRejectionNotificationAsync(mentorProfile.User.Email, fullName, rejectReason);
                        _logger.LogInformation("Sent rejection email after re-evaluation for user {UserId}", userId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email after re-evaluation for user {UserId}", userId);
                }
            }

            // Return updated application detail
            var detailResult = await GetMentorApplicationDetailAsync(userId);
            if (detailResult.Success)
                return ServiceResult<MentorApplicationDto>.SuccessResult(detailResult.Data!, resultMessage);

            return ServiceResult<MentorApplicationDto>.SuccessResult(null!, resultMessage);
        }

        #endregion

        #region Mentor Application History

        public async Task<ServiceResult<PagedResponse<MentorApplicationHistoryDto>>> GetApplicationHistoryAsync(ApplicationHistoryQueryParams queryParams)
        {
            var historyItems = await _unitOfWork.MentorApplicationHistories.GetAllAsync();
            var query = historyItems.AsQueryable();

            // Apply filters
            if (queryParams.UserId.HasValue)
                query = query.Where(h => h.UserId == queryParams.UserId.Value);

            if (queryParams.Action.HasValue)
                query = query.Where(h => h.Action == queryParams.Action.Value);

            if (queryParams.FromDate.HasValue)
                query = query.Where(h => h.CreatedAt >= queryParams.FromDate.Value);

            if (queryParams.ToDate.HasValue)
                query = query.Where(h => h.CreatedAt <= queryParams.ToDate.Value);

            var totalCount = query.Count();

            var items = query
                .OrderByDescending(h => h.CreatedAt)
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToList();

            // Get user information for each history item
            var userIds = items.Select(h => h.UserId).Distinct().ToList();
            var creatorIds = items.Where(h => h.CreatedBy.HasValue).Select(h => h.CreatedBy!.Value).Distinct().ToList();
            var allUserIds = userIds.Union(creatorIds).ToList();

            var users = new Dictionary<long, User>();
            foreach (var uid in allUserIds)
            {
                var user = await _unitOfWork.Users.GetByIdWithProfilesAsync(uid);
                if (user != null)
                    users[uid] = user;
            }

            var dtoList = items.Select(h => new MentorApplicationHistoryDto
            {
                Id = h.Id,
                UserId = h.UserId,
                UserEmail = users.ContainsKey(h.UserId) ? users[h.UserId].Email : null,
                UserFullName = users.ContainsKey(h.UserId) ? users[h.UserId].UserProfile?.FullName : null,
                Action = h.Action,
                ApplicationSnapshot = h.ApplicationSnapshot,
                CertificationUrls = h.CertificationUrls,
                Reason = h.Reason,
                AiConfidenceScore = h.AiConfidenceScore,
                AiModelVersion = h.AiModelVersion,
                SubmissionCount = h.SubmissionCount,
                CreatedAt = h.CreatedAt,
                CreatedById = h.CreatedBy,
                CreatedByEmail = h.CreatedBy.HasValue && users.ContainsKey(h.CreatedBy.Value) ? users[h.CreatedBy.Value].Email : null
            }).ToList();

            var pagedResponse = PagedResponse<MentorApplicationHistoryDto>.Create(
                dtoList,
                queryParams.PageNumber,
                queryParams.PageSize,
                totalCount
            );

            return ServiceResult<PagedResponse<MentorApplicationHistoryDto>>.SuccessResult(pagedResponse);
        }

        public async Task<ServiceResult<List<MentorApplicationHistoryDto>>> GetUserApplicationHistoryAsync(long userId)
        {
            var historyItems = await _unitOfWork.MentorApplicationHistories.GetByUserIdAsync(userId);

            var user = await _unitOfWork.Users.GetByIdWithProfilesAsync(userId);
            var creatorIds = historyItems.Where(h => h.CreatedBy.HasValue).Select(h => h.CreatedBy!.Value).Distinct().ToList();

            var creators = new Dictionary<long, User>();
            foreach (var cid in creatorIds)
            {
                var creator = await _unitOfWork.Users.GetByIdWithProfilesAsync(cid);
                if (creator != null)
                    creators[cid] = creator;
            }

            var dtoList = historyItems.Select(h => new MentorApplicationHistoryDto
            {
                Id = h.Id,
                UserId = h.UserId,
                UserEmail = user?.Email,
                UserFullName = user?.UserProfile?.FullName,
                Action = h.Action,
                ApplicationSnapshot = h.ApplicationSnapshot,
                CertificationUrls = h.CertificationUrls,
                Reason = h.Reason,
                AiConfidenceScore = h.AiConfidenceScore,
                AiModelVersion = h.AiModelVersion,
                SubmissionCount = h.SubmissionCount,
                CreatedAt = h.CreatedAt,
                CreatedById = h.CreatedBy,
                CreatedByEmail = h.CreatedBy.HasValue && creators.ContainsKey(h.CreatedBy.Value) ? creators[h.CreatedBy.Value].Email : null
            }).ToList();

            return ServiceResult<List<MentorApplicationHistoryDto>>.SuccessResult(dtoList);
        }

        public async Task<ServiceResult<List<MentorApplicationHistoryExportDto>>> ExportApplicationHistoryForMLAsync(
            DateTime? fromDate = null, 
            DateTime? toDate = null, 
            ApplicationAction? action = null)
        {
            var historyItems = await _unitOfWork.MentorApplicationHistories.GetAllAsync();
            var query = historyItems.AsQueryable();

            // Apply filters
            if (fromDate.HasValue)
                query = query.Where(h => h.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(h => h.CreatedAt <= toDate.Value);

            if (action.HasValue)
                query = query.Where(h => h.Action == action.Value);

            // Only export reviewed applications (Approved or Rejected), not Submitted
            query = query.Where(h => h.Action == ApplicationAction.Approved || h.Action == ApplicationAction.Rejected);

            var exportList = query
                .OrderBy(h => h.CreatedAt)
                .Select(h => new MentorApplicationHistoryExportDto
                {
                    UserId = h.UserId,
                    Action = (int)h.Action,
                    ApplicationSnapshot = h.ApplicationSnapshot,
                    CertificationUrls = h.CertificationUrls,
                    Reason = h.Reason,
                    SubmissionCount = h.SubmissionCount,
                    CreatedAt = h.CreatedAt,
                    IsManualReview = h.CreatedBy.HasValue,  // If CreatedBy is set, it was manual review
                    AiConfidenceScore = h.AiConfidenceScore
                })
                .ToList();

            return ServiceResult<List<MentorApplicationHistoryExportDto>>.SuccessResult(exportList);
        }

        #endregion

        #region Private Methods

        private CategoryResponseDto MapToCategoryDto(Category category)
        {
            return new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
                // No more parent-child hierarchy
            };
        }

        private static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        #endregion
    }
}
