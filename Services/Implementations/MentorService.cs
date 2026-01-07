using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Review;
using MentorEdu.DTOs.User;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using MentorEdu.Services.Common;
using MentorEdu.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Services.Implementations
{
    public class MentorService : BaseService, IMentorService
    {
        public MentorService(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
        }

        public async Task<ServiceResult<PagedResponse<MentorListDto>>> SearchMentorsAsync(SearchMentorRequest request)
        {
            var query = _unitOfWork.Users.Query()
                .Where(u => u.Role == UserRole.Mentor && u.IsActive && u.IsVerified);

            // Filter by keyword
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.ToLower();
                query = query.Where(u => 
                    (u.UserProfile != null && u.UserProfile.FullName != null && u.UserProfile.FullName.ToLower().Contains(keyword)) ||
                    (u.MentorProfile != null && u.MentorProfile.Title != null && u.MentorProfile.Title.ToLower().Contains(keyword)) ||
                    (u.MentorProfile != null && u.MentorProfile.Introduction != null && u.MentorProfile.Introduction.ToLower().Contains(keyword)));
            }

            // Filter by category
            if (request.CategoryId.HasValue)
            {
                query = query.Where(u => u.MentorCategories.Any(mc => mc.CategoryId == request.CategoryId));
            }

            // Filter by hashtags
            if (request.HashtagIds != null && request.HashtagIds.Any())
            {
                query = query.Where(u => u.MentorHashtags.Any(mh => request.HashtagIds.Contains(mh.HashtagId)));
            }

            // Filter by hourly rate
            if (request.MinHourlyRate.HasValue)
            {
                query = query.Where(u => u.MentorProfile != null && u.MentorProfile.HourlyRate >= request.MinHourlyRate);
            }

            if (request.MaxHourlyRate.HasValue)
            {
                query = query.Where(u => u.MentorProfile != null && u.MentorProfile.HourlyRate <= request.MaxHourlyRate);
            }

            // Filter by experience
            if (request.MinExperienceYears.HasValue)
            {
                query = query.Where(u => u.MentorProfile != null && u.MentorProfile.ExperienceYears >= request.MinExperienceYears);
            }

            // Filter by rating
            if (request.MinRating.HasValue)
            {
                query = query.Where(u => u.MentorProfile != null && u.MentorProfile.RatingAvg >= request.MinRating);
            }

            // Filter by city
            if (!string.IsNullOrWhiteSpace(request.City))
            {
                query = query.Where(u => u.UserProfile != null && u.UserProfile.City == request.City);
            }

            // Count total
            var totalCount = await query.CountAsync();

            // Sorting
            query = request.SortBy?.ToLower() switch
            {
                "price" => request.SortDescending 
                    ? query.OrderByDescending(u => u.MentorProfile!.HourlyRate)
                    : query.OrderBy(u => u.MentorProfile!.HourlyRate),
                "experience" => request.SortDescending 
                    ? query.OrderByDescending(u => u.MentorProfile!.ExperienceYears)
                    : query.OrderBy(u => u.MentorProfile!.ExperienceYears),
                _ => request.SortDescending 
                    ? query.OrderByDescending(u => u.MentorProfile!.RatingAvg)
                    : query.OrderBy(u => u.MentorProfile!.RatingAvg)
            };

            // Pagination
            var mentors = await query
                .Include(u => u.UserProfile)
                .Include(u => u.MentorProfile)
                .Include(u => u.MentorCategories)
                    .ThenInclude(mc => mc.Category)
                .Include(u => u.MentorHashtags)
                    .ThenInclude(mh => mh.Hashtag)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var dtos = mentors.Select(m => new MentorListDto
            {
                Id = m.Id,
                FullName = m.UserProfile?.FullName,
                AvatarUrl = m.UserProfile?.AvatarUrl,
                Title = m.MentorProfile?.Title,
                HourlyRate = m.MentorProfile?.HourlyRate,
                ExperienceYears = m.MentorProfile?.ExperienceYears,
                RatingAvg = m.MentorProfile?.RatingAvg,
                RatingCount = m.MentorProfile?.RatingCount,
                School = m.UserProfile?.School,
                City = m.UserProfile?.City,
                Categories = m.MentorCategories?.Select(mc => mc.Category?.Name ?? "").ToList(),
                Hashtags = m.MentorHashtags?.Select(mh => mh.Hashtag?.Name ?? "").ToList()
            });

            var response = PagedResponse<MentorListDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount);
            return ServiceResult<PagedResponse<MentorListDto>>.SuccessResult(response);
        }

        public async Task<ServiceResult<List<MentorListDto>>> GetTopMentorsAsync(int count = 10)
        {
            var mentorProfiles = await _unitOfWork.MentorProfiles.GetTopRatedMentorsAsync(count);

            var dtos = mentorProfiles.Select(mp => new MentorListDto
            {
                Id = mp.UserId,
                FullName = mp.User?.UserProfile?.FullName,
                AvatarUrl = mp.User?.UserProfile?.AvatarUrl,
                Title = mp.Title,
                HourlyRate = mp.HourlyRate,
                ExperienceYears = mp.ExperienceYears,
                RatingAvg = mp.RatingAvg,
                RatingCount = mp.RatingCount,
                School = mp.User?.UserProfile?.School,
                City = mp.User?.UserProfile?.City
            }).ToList();

            return ServiceResult<List<MentorListDto>>.SuccessResult(dtos);
        }

        public async Task<ServiceResult<List<MentorListDto>>> GetMentorsByCategoryAsync(long categoryId)
        {
            var mentorProfiles = await _unitOfWork.MentorProfiles.GetMentorsByCategoryAsync(categoryId);

            var dtos = mentorProfiles.Select(mp => new MentorListDto
            {
                Id = mp.UserId,
                FullName = mp.User?.UserProfile?.FullName,
                AvatarUrl = mp.User?.UserProfile?.AvatarUrl,
                Title = mp.Title,
                HourlyRate = mp.HourlyRate,
                ExperienceYears = mp.ExperienceYears,
                RatingAvg = mp.RatingAvg,
                RatingCount = mp.RatingCount,
                School = mp.User?.UserProfile?.School,
                City = mp.User?.UserProfile?.City
            }).ToList();

            return ServiceResult<List<MentorListDto>>.SuccessResult(dtos);
        }

        public async Task<ServiceResult<UserDetailDto>> GetMentorDetailAsync(long mentorId)
        {
            var user = await _unitOfWork.Users.GetByIdWithProfilesAsync(mentorId);
            
            if (user == null || user.Role != UserRole.Mentor)
                return ServiceResult<UserDetailDto>.NotFoundResult("Mentor not found");

            if (!user.IsActive)
                return ServiceResult<UserDetailDto>.NotFoundResult("Mentor is no longer active");

            var mentorProfile = await _unitOfWork.MentorProfiles.GetByUserIdWithDetailsAsync(mentorId);

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
                Country = user.UserProfile?.Country,
                MentorProfile = mentorProfile != null ? new MentorProfileDto
                {
                    Title = mentorProfile.Title,
                    HourlyRate = mentorProfile.HourlyRate,
                    PackagePrice = mentorProfile.PackagePrice,
                    ExperienceYears = mentorProfile.ExperienceYears,
                    Introduction = mentorProfile.Introduction,
                    AvailabilityNote = mentorProfile.AvailabilityNote,
                    RatingAvg = mentorProfile.RatingAvg,
                    RatingCount = mentorProfile.RatingCount,
                    Categories = mentorProfile.User?.MentorCategories?.Select(mc => new CategoryDto
                    {
                        Id = mc.CategoryId,
                        Name = mc.Category?.Name
                    }).ToList(),
                    Hashtags = mentorProfile.User?.MentorHashtags?.Select(mh => new HashtagDto
                    {
                        Id = mh.HashtagId,
                        Name = mh.Hashtag?.Name
                    }).ToList()
                } : null
            };

            return ServiceResult<UserDetailDto>.SuccessResult(dto);
        }

        public async Task<ServiceResult<PagedResponse<MentorReviewDto>>> GetMentorReviewsAsync(long mentorId, int pageNumber, int pageSize)
        {
            var (reviews, totalCount) = await _unitOfWork.Reviews.GetPagedByUserAsync(mentorId, pageNumber, pageSize);

            var dtos = reviews.Select(r => new MentorReviewDto
            {
                Id = r.Id,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                StudentName = r.FromUser?.UserProfile?.FullName,
                StudentAvatar = r.FromUser?.UserProfile?.AvatarUrl,
                OrderTitle = r.Order?.Request?.Title
            });

            var response = PagedResponse<MentorReviewDto>.Create(dtos, pageNumber, pageSize, totalCount);
            return ServiceResult<PagedResponse<MentorReviewDto>>.SuccessResult(response);
        }

        public async Task<ServiceResult<ReviewSummaryDto>> GetMentorReviewSummaryAsync(long mentorId)
        {
            var reviews = await _unitOfWork.Reviews.GetByToUserIdAsync(mentorId);
            var reviewList = reviews.ToList();

            var summary = new ReviewSummaryDto
            {
                UserId = mentorId,
                TotalReviews = reviewList.Count,
                AverageRating = reviewList.Any() ? reviewList.Average(r => r.Rating) : 0,
                FiveStarCount = reviewList.Count(r => r.Rating == 5),
                FourStarCount = reviewList.Count(r => r.Rating == 4),
                ThreeStarCount = reviewList.Count(r => r.Rating == 3),
                TwoStarCount = reviewList.Count(r => r.Rating == 2),
                OneStarCount = reviewList.Count(r => r.Rating == 1)
            };

            return ServiceResult<ReviewSummaryDto>.SuccessResult(summary);
        }
    }
}
