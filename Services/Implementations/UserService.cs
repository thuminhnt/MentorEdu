using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.User;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using MentorEdu.Services.Common;
using MentorEdu.Services.Interfaces;

namespace MentorEdu.Services.Implementations
{
    public class UserService : BaseService, IUserService
    {
        public UserService(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
        }

        public async Task<ServiceResult<UserDetailDto>> GetUserByIdAsync(long userId)
        {
            var user = await _unitOfWork.Users.GetByIdWithProfilesAsync(userId);
            
            if (user == null)
                return ServiceResult<UserDetailDto>.NotFoundResult("User not found");

            var dto = await MapToUserDetailDto(user);
            return ServiceResult<UserDetailDto>.SuccessResult(dto);
        }

        public async Task<ServiceResult<UserDetailDto>> GetCurrentUserProfileAsync(long currentUserId)
        {
            return await GetUserByIdAsync(currentUserId);
        }

        public async Task<ServiceResult<UserDetailDto>> UpdateUserProfileAsync(long userId, UpdateUserProfileRequest request)
        {
            var userProfile = await _unitOfWork.UserProfiles.GetByUserIdAsync(userId);
            
            if (userProfile == null)
            {
                // Create new profile if not exists
                userProfile = new UserProfile
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.UserProfiles.AddAsync(userProfile);
            }

            // Update fields
            userProfile.FullName = request.FullName ?? userProfile.FullName;
            userProfile.AvatarUrl = request.AvatarUrl ?? userProfile.AvatarUrl;
            userProfile.Phone = request.Phone ?? userProfile.Phone;
            userProfile.Gender = request.Gender ?? userProfile.Gender;
            userProfile.School = request.School ?? userProfile.School;
            userProfile.Major = request.Major ?? userProfile.Major;
            userProfile.Bio = request.Bio ?? userProfile.Bio;
            userProfile.City = request.City ?? userProfile.City;
            userProfile.Country = request.Country ?? userProfile.Country;
            userProfile.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            return await GetUserByIdAsync(userId);
        }

        public async Task<ServiceResult<UserDetailDto>> UpdateMentorProfileAsync(long userId, UpdateMentorProfileRequest request)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            
            if (user == null)
                return ServiceResult<UserDetailDto>.NotFoundResult("User not found");

            if (user.Role != UserRole.Mentor)
                return ServiceResult<UserDetailDto>.FailResult("Only mentors can update this information");

            var mentorProfile = await _unitOfWork.MentorProfiles.GetByUserIdAsync(userId);
            
            if (mentorProfile == null)
            {
                mentorProfile = new MentorProfile
                {
                    UserId = userId,
                    RatingAvg = 0,
                    RatingCount = 0,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.MentorProfiles.AddAsync(mentorProfile);
            }

            // Update mentor profile
            mentorProfile.Title = request.Title ?? mentorProfile.Title;
            mentorProfile.HourlyRate = request.HourlyRate ?? mentorProfile.HourlyRate;
            mentorProfile.PackagePrice = request.PackagePrice ?? mentorProfile.PackagePrice;
            mentorProfile.ExperienceYears = request.ExperienceYears ?? mentorProfile.ExperienceYears;
            mentorProfile.Introduction = request.Introduction ?? mentorProfile.Introduction;
            mentorProfile.AvailabilityNote = request.AvailabilityNote ?? mentorProfile.AvailabilityNote;
            mentorProfile.UpdatedAt = DateTime.UtcNow;

            // Update categories
            if (request.CategoryIds != null && request.CategoryIds.Any())
            {
                // Remove old categories
                var existingCategories = await _unitOfWork.Categories
                    .FindAsync(c => user.MentorCategories.Select(mc => mc.CategoryId).Contains(c.Id));
                
                user.MentorCategories.Clear();
                
                // Add new categories
                foreach (var categoryId in request.CategoryIds)
                {
                    user.MentorCategories.Add(new MentorCategory
                    {
                        MentorId = userId,
                        CategoryId = categoryId
                    });
                }
            }

            // Update hashtags
            if (request.HashtagIds != null && request.HashtagIds.Any())
            {
                user.MentorHashtags.Clear();
                
                foreach (var hashtagId in request.HashtagIds)
                {
                    user.MentorHashtags.Add(new MentorHashtag
                    {
                        MentorId = userId,
                        HashtagId = hashtagId
                    });
                }
            }

            // Add new hashtags
            if (request.NewHashtags != null && request.NewHashtags.Any())
            {
                foreach (var hashtagName in request.NewHashtags)
                {
                    var existingHashtag = await _unitOfWork.Hashtags.GetByNameAsync(hashtagName);
                    
                    if (existingHashtag == null)
                    {
                        existingHashtag = new Hashtag { Name = hashtagName };
                        await _unitOfWork.Hashtags.AddAsync(existingHashtag);
                        await _unitOfWork.SaveChangesAsync();
                    }

                    if (!user.MentorHashtags.Any(mh => mh.HashtagId == existingHashtag.Id))
                    {
                        user.MentorHashtags.Add(new MentorHashtag
                        {
                            MentorId = userId,
                            HashtagId = existingHashtag.Id
                        });
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();

            return await GetUserByIdAsync(userId);
        }

        public async Task<ServiceResult<string>> UpdateAvatarAsync(long userId, string avatarUrl)
        {
            var userProfile = await _unitOfWork.UserProfiles.GetByUserIdAsync(userId);
            
            if (userProfile == null)
            {
                userProfile = new UserProfile
                {
                    UserId = userId,
                    AvatarUrl = avatarUrl,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.UserProfiles.AddAsync(userProfile);
            }
            else
            {
                userProfile.AvatarUrl = avatarUrl;
                userProfile.UpdatedAt = DateTime.UtcNow;
            }

            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<string>.SuccessResult(avatarUrl, "Avatar updated successfully");
        }

        public async Task<ServiceResult<PagedResponse<UserBasicDto>>> GetStudentsAsync(int pageNumber, int pageSize)
        {
            var students = await _unitOfWork.Users.GetStudentsAsync();
            var totalCount = students.Count();

            var pagedStudents = students
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new UserBasicDto
                {
                    Id = s.Id,
                    Email = s.Email,
                    FullName = s.UserProfile?.FullName,
                    AvatarUrl = s.UserProfile?.AvatarUrl,
                    Role = s.Role,
                    IsVerified = s.IsVerified,
                    IsActive = s.IsActive
                });

            var response = PagedResponse<UserBasicDto>.Create(pagedStudents, pageNumber, pageSize, totalCount);
            return ServiceResult<PagedResponse<UserBasicDto>>.SuccessResult(response);
        }

        public async Task<ServiceResult> DeactivateAccountAsync(long userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            
            if (user == null)
                return ServiceResult.NotFoundResult("User not found");

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Account deactivated successfully");
        }

        public async Task<ServiceResult> DeleteAccountAsync(long userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            
            if (user == null)
                return ServiceResult.NotFoundResult("User not found");

            // Soft delete - just deactivate
            user.IsActive = false;
            user.Email = $"deleted_{user.Id}_{user.Email}"; // Anonymize email
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Account deleted successfully");
        }

        public async Task<ServiceResult> ApplyAsMentorAsync(long userId, ApplyAsMentorRequest request)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            
            if (user == null)
                return ServiceResult.NotFoundResult("User not found");

            // Check if already a mentor
            if (user.Role == UserRole.Mentor)
                return ServiceResult.FailResult("Bạn đã là Mentor rồi");

            // Check if already has pending application
            var existingProfile = await _unitOfWork.MentorProfiles.GetByUserIdAsync(userId);
            if (existingProfile != null)
            {
                if (existingProfile.ApprovalStatus == MentorApprovalStatus.Pending)
                    return ServiceResult.FailResult("Bạn đã có đơn đăng ký đang chờ duyệt");
                if (existingProfile.ApprovalStatus == MentorApprovalStatus.Approved)
                    return ServiceResult.FailResult("Đơn đăng ký của bạn đã được duyệt");
            }

            // Create or update mentor profile
            if (existingProfile == null)
            {
                existingProfile = new MentorProfile
                {
                    UserId = userId,
                    RatingAvg = 0,
                    RatingCount = 0,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.MentorProfiles.AddAsync(existingProfile);
            }

            // Update with application data
            existingProfile.ApprovalStatus = MentorApprovalStatus.Pending;
            existingProfile.Specialization = request.Specialization;
            existingProfile.ExperienceYears = request.ExperienceYears;
            existingProfile.Introduction = request.Introduction;
            existingProfile.PortfolioUrl = request.PortfolioUrl;
            existingProfile.CertificationUrls = request.CertificationUrls;
            existingProfile.ApplicationNote = request.ApplicationNote;
            existingProfile.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Đơn đăng ký mentor đã được gửi. Vui lòng chờ admin phê duyệt.");
        }

        public async Task<ServiceResult<MentorApplicationStatusDto>> GetMentorApplicationStatusAsync(long userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            
            if (user == null)
                return ServiceResult<MentorApplicationStatusDto>.NotFoundResult("User not found");

            // Already a mentor
            if (user.Role == UserRole.Mentor)
            {
                return ServiceResult<MentorApplicationStatusDto>.SuccessResult(new MentorApplicationStatusDto
                {
                    HasApplied = true,
                    Status = MentorApprovalStatus.Approved,
                    StatusText = "Bạn đã là Mentor"
                });
            }

            var mentorProfile = await _unitOfWork.MentorProfiles.GetByUserIdAsync(userId);
            
            if (mentorProfile == null)
            {
                return ServiceResult<MentorApplicationStatusDto>.SuccessResult(new MentorApplicationStatusDto
                {
                    HasApplied = false,
                    StatusText = "Bạn chưa đăng ký làm Mentor"
                });
            }

            var statusText = mentorProfile.ApprovalStatus switch
            {
                MentorApprovalStatus.Pending => "Đơn đăng ký đang chờ duyệt",
                MentorApprovalStatus.Approved => "Đơn đăng ký đã được duyệt",
                MentorApprovalStatus.Rejected => $"Đơn đăng ký bị từ chối: {mentorProfile.RejectionReason}",
                _ => "Không xác định"
            };

            return ServiceResult<MentorApplicationStatusDto>.SuccessResult(new MentorApplicationStatusDto
            {
                HasApplied = true,
                Status = mentorProfile.ApprovalStatus,
                StatusText = statusText,
                AppliedAt = mentorProfile.CreatedAt,
                ReviewedAt = mentorProfile.ReviewedAt,
                RejectionReason = mentorProfile.RejectionReason,
                Specialization = mentorProfile.Specialization,
                ExperienceYears = mentorProfile.ExperienceYears
            });
        }

        #region Private Methods

        private async Task<UserDetailDto> MapToUserDetailDto(User user)
        {
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
                var mentorProfile = await _unitOfWork.MentorProfiles.GetByUserIdWithDetailsAsync(user.Id);
                
                dto.MentorProfile = new MentorProfileDto
                {
                    Title = mentorProfile?.Title,
                    HourlyRate = mentorProfile?.HourlyRate,
                    PackagePrice = mentorProfile?.PackagePrice,
                    ExperienceYears = mentorProfile?.ExperienceYears,
                    Introduction = mentorProfile?.Introduction,
                    AvailabilityNote = mentorProfile?.AvailabilityNote,
                    RatingAvg = mentorProfile?.RatingAvg,
                    RatingCount = mentorProfile?.RatingCount,
                    Categories = user.MentorCategories?.Select(mc => new CategoryDto
                    {
                        Id = mc.CategoryId,
                        Name = mc.Category?.Name
                    }).ToList(),
                    Hashtags = user.MentorHashtags?.Select(mh => new HashtagDto
                    {
                        Id = mh.HashtagId,
                        Name = mh.Hashtag?.Name
                    }).ToList()
                };
            }

            return dto;
        }

        #endregion
    }
}
