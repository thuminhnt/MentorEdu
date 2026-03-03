using MentorEdu.DTOs.Admin;
using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.User;
using MentorEdu.Extensions;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using MentorEdu.Services.Common;
using MentorEdu.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Services.Implementations
{
    public class AdminUserService : BaseService, IAdminUserService
    {
        public AdminUserService(IUnitOfWork unitOfWork) : base(unitOfWork) { }

        public async Task<ServiceResult<PagedResponse<UserBasicDto>>> GetUsersAsync(
            int pageNumber, int pageSize, UserRole? role = null, bool? isActive = null, string? keyword = null)
        {
            var query = _unitOfWork.Users.Query();

            if (role.HasValue)
                query = query.Where(u => u.Role == role.Value);

            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(u => u.Email.Contains(keyword) ||
                    (u.UserProfile != null && u.UserProfile.FullName != null && u.UserProfile.FullName.Contains(keyword)));

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

            return ServiceResult<UserDetailDto>.SuccessResult(user.ToDetailDto());
        }

        public async Task<ServiceResult<UserDetailDto>> AdminCreateUserAsync(AdminCreateUserDto request)
        {
            var existingUser = await _unitOfWork.Users.GetByEmailAsync(request.Email);
            if (existingUser != null)
                return ServiceResult<UserDetailDto>.FailResult("Email already exists");

            return await _unitOfWork.ExecuteWithStrategyAsync(async () =>
            {
                var user = new User
                {
                    Email = request.Email,
                    PasswordHash = PasswordHelper.Hash(request.Password),
                    Role = request.Role,
                    IsVerified = request.IsVerified,
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

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

                user.UserProfile = userProfile;
                return ServiceResult<UserDetailDto>.CreatedResult(user.ToDetailDto(), "User created successfully");
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

            if (user.Email != request.Email)
            {
                var existingUser = await _unitOfWork.Users.GetByEmailAsync(request.Email);
                if (existingUser != null && existingUser.Id != userId)
                    return ServiceResult<UserDetailDto>.FailResult("Email already exists");
            }

            user.Email = request.Email;
            user.IsVerified = request.IsVerified;
            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.Password))
                user.PasswordHash = PasswordHelper.Hash(request.Password);

            _unitOfWork.Users.Update(user);

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

            return ServiceResult<UserDetailDto>.SuccessResult(user.ToDetailDto(), "User updated successfully");
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

            var hasActiveOrders = await _unitOfWork.Orders.Query()
                .AnyAsync(o => (o.StudentId == userId || o.MentorId == userId) &&
                               (o.Status == OrderStatus.InProgress || o.Status == OrderStatus.PendingPayment));

            if (hasActiveOrders)
                return ServiceResult.FailResult("Cannot delete user with active orders");

            return await _unitOfWork.ExecuteWithStrategyAsync(async () =>
            {
                if (user.UserProfile != null)
                    _unitOfWork.UserProfiles.Remove(user.UserProfile);

                if (user.MentorProfile != null)
                    _unitOfWork.MentorProfiles.Remove(user.MentorProfile);

                if (user.Wallet != null)
                {
                    if (user.Wallet.Balance > 0)
                        return ServiceResult.FailResult("Cannot delete user with positive wallet balance");
                    _unitOfWork.Wallets.Remove(user.Wallet);
                }

                _unitOfWork.Users.Remove(user);
                await _unitOfWork.SaveChangesAsync();

                return ServiceResult.SuccessResult("User deleted successfully");
            });
        }

        public async Task<ServiceResult> ActivateUserAsync(long userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null) return ServiceResult.NotFoundResult("User not found");

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Account activated successfully");
        }

        public async Task<ServiceResult> DeactivateUserAsync(long userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null) return ServiceResult.NotFoundResult("User not found");

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
            if (user == null) return ServiceResult.NotFoundResult("User not found");

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
            if (user == null) return ServiceResult.NotFoundResult("User not found");

            user.IsVerified = true;
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Account verified successfully");
        }
    }
}
