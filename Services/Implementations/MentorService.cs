using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Review;
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
    public class MentorService : BaseService, IMentorService
    {
        private readonly IGeminiAiService _geminiAiService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MentorService> _logger;

        public MentorService(
            IUnitOfWork unitOfWork,
            IGeminiAiService geminiAiService,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<MentorService> logger) : base(unitOfWork)
        {
            _geminiAiService = geminiAiService;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
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
                StudentId = r.FromUserId,
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

        #region Mentor Application Flow

        public async Task<ServiceResult> ApplyAsMentorAsync(long userId, ApplyAsMentorRequest request)
        {
            // Check if user exists and is a Student
            var user = await _unitOfWork.Users.GetByIdWithProfilesAsync(userId);
            if (user == null)
                return ServiceResult.NotFoundResult("User not found");

            if (user.Role != UserRole.Student)
                return ServiceResult.FailResult("Only students can apply to become a mentor");

            // Check if already has mentor profile
            var existingProfile = await _unitOfWork.MentorProfiles.GetByUserIdAsync(userId);
            if (existingProfile != null)
            {
                if (existingProfile.ApprovalStatus == MentorApprovalStatus.Pending)
                    return ServiceResult.FailResult("You already have a pending application");
                
                if (existingProfile.ApprovalStatus == MentorApprovalStatus.Approved)
                    return ServiceResult.FailResult("Your application has already been approved. Please confirm to switch to Mentor role.");
            }

            // Get submission count for history
            var submissionCount = await _unitOfWork.MentorApplicationHistories.GetSubmissionCountAsync(userId) + 1;

            // Perform AI analysis BEFORE transaction
            GeminiAnalysisResult? aiResult = null;
            try
            {
                if (_geminiAiService.IsConfigured())
                {
                    _logger.LogInformation("Starting AI analysis for user {UserId}", userId);
                    aiResult = await _geminiAiService.AnalyzeMentorApplicationAsync(
                        userId,
                        request,
                        request.CertificationUrls
                    );
                    _logger.LogInformation(
                        "AI Analysis completed for user {UserId}: Score={Score}, Action={Action}",
                        userId, aiResult.ConfidenceScore, aiResult.RecommendedAction);
                }
                else
                {
                    _logger.LogWarning("Gemini AI is not configured. Proceeding with manual review for user {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI Analysis failed for user {UserId}, proceeding with manual review", userId);
            }

            string resultMessage = "Mentor application submitted successfully. Please wait for admin review.";

            // Use ExecuteWithStrategyAsync to properly handle transactions with retry strategy
            try
            {
            await _unitOfWork.ExecuteWithStrategyAsync(async () =>
            {
                MentorProfile currentProfile;

                // If rejected before, update the existing profile
                if (existingProfile != null && existingProfile.ApprovalStatus == MentorApprovalStatus.Rejected)
                {
                    existingProfile.Specialization = request.Specialization;
                    existingProfile.ExperienceYears = request.ExperienceYears;
                    existingProfile.Introduction = request.Introduction;
                    existingProfile.PortfolioUrl = request.PortfolioUrl;
                    existingProfile.CertificationUrls = request.CertificationUrls;
                    existingProfile.ApplicationNote = request.ApplicationNote;
                    existingProfile.ApprovalStatus = MentorApprovalStatus.Pending;
                    existingProfile.RejectionReason = null;
                    existingProfile.ReviewedAt = null;
                    existingProfile.ReviewedBy = null;
                    existingProfile.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.MentorProfiles.Update(existingProfile);
                    currentProfile = existingProfile;

                    // Update categories - remove old and add new
                    var existingCategories = await _unitOfWork.MentorCategories.Query()
                        .Where(mc => mc.MentorId == userId).ToListAsync();
                    foreach (var cat in existingCategories)
                        _unitOfWork.MentorCategories.Remove(cat);

                    // Update hashtags - remove old and add new
                    var existingHashtags = await _unitOfWork.MentorHashtags.Query()
                        .Where(mh => mh.MentorId == userId).ToListAsync();
                    foreach (var hash in existingHashtags)
                        _unitOfWork.MentorHashtags.Remove(hash);
                }
                else
                {
                    // Create new mentor profile
                    currentProfile = new MentorProfile
                    {
                        UserId = userId,
                        Specialization = request.Specialization,
                        ExperienceYears = request.ExperienceYears,
                        Introduction = request.Introduction,
                        PortfolioUrl = request.PortfolioUrl,
                        CertificationUrls = request.CertificationUrls,
                        ApplicationNote = request.ApplicationNote,
                        ApprovalStatus = MentorApprovalStatus.Pending,
                        HourlyRate = 0,
                        RatingAvg = 0,
                        RatingCount = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.MentorProfiles.AddAsync(currentProfile);
                }

                // Add new mentor categories
                if (request.CategoryIds != null && request.CategoryIds.Any())
                {
                    foreach (var catId in request.CategoryIds.Distinct())
                    {
                        var mentorCategory = new MentorCategory
                        {
                            MentorId = userId,
                            CategoryId = catId
                        };
                        await _unitOfWork.MentorCategories.AddAsync(mentorCategory);
                    }
                }

                // Add new mentor hashtags (optional)
                if (request.HashtagIds != null && request.HashtagIds.Any())
                {
                    foreach (var hashId in request.HashtagIds.Distinct())
                    {
                        var mentorHashtag = new MentorHashtag
                        {
                            MentorId = userId,
                            HashtagId = hashId
                        };
                        await _unitOfWork.MentorHashtags.AddAsync(mentorHashtag);
                    }
                }

                // Prepare AI analysis data for snapshot
                var aiAnalysisData = aiResult != null ? new
                {
                    AiConfidenceScore = aiResult.ConfidenceScore,
                    AiRecommendedAction = aiResult.RecommendedAction,
                    AiReasoning = aiResult.Reasoning,
                    AiKeyPoints = aiResult.KeyPoints,
                    AiRedFlags = aiResult.RedFlags,
                    AiModelVersion = aiResult.ModelVersion
                } : null;

                // Record history for audit trail and ML training
                var applicationSnapshot = JsonSerializer.Serialize(new
                {
                    request.Specialization,
                    request.ExperienceYears,
                    request.Introduction,
                    request.PortfolioUrl,
                    request.ApplicationNote,
                    request.CategoryIds,
                    request.HashtagIds,
                    AiAnalysis = aiAnalysisData
                });

                // Determine action based on AI result
                ApplicationAction historyAction = ApplicationAction.Submitted;
                string? historyReason = null;

                if (aiResult != null && _geminiAiService.IsConfigured())
                {
                    if (aiResult.RecommendedAction == "AUTO_APPROVE")
                    {
                        historyAction = ApplicationAction.AutoApproved;
                        historyReason = $"Auto-approved by AI (Score: {aiResult.ConfidenceScore}%). {aiResult.Reasoning}";
                        
                        // Auto-approve the application
                        // Note: Do NOT call Update() here — the entity may be in Added state
                        // (first-time application). Calling Update() would change state to Modified,
                        // causing EF Core to generate UPDATE instead of INSERT → DbUpdateConcurrencyException.
                        // EF Core's change tracker automatically detects property changes on tracked entities.
                        currentProfile.ApprovalStatus = MentorApprovalStatus.Approved;
                        currentProfile.ReviewedAt = DateTime.UtcNow;
                        currentProfile.UpdatedAt = DateTime.UtcNow;

                        resultMessage = $"Congratulations! Your application has been automatically approved (AI Confidence: {aiResult.ConfidenceScore}%). Please go to your Profile to confirm switching to Mentor role.";
                    }
                    else if (aiResult.RecommendedAction == "AUTO_REJECT")
                    {
                        historyAction = ApplicationAction.AutoRejected;
                        var redFlagsText = aiResult.RedFlags.Any() 
                            ? string.Join("; ", aiResult.RedFlags) 
                            : "Application did not meet minimum requirements";
                        historyReason = $"Auto-rejected by AI (Score: {aiResult.ConfidenceScore}%). Concerns: {redFlagsText}";
                        
                        // Auto-reject the application
                        // Note: Do NOT call Update() here — same reason as AUTO_APPROVE above.
                        currentProfile.ApprovalStatus = MentorApprovalStatus.Rejected;
                        currentProfile.RejectionReason = $"Your application was automatically reviewed. Concerns identified: {redFlagsText}. You may reapply with improved credentials.";
                        currentProfile.ReviewedAt = DateTime.UtcNow;
                        currentProfile.UpdatedAt = DateTime.UtcNow;

                        resultMessage = $"Unfortunately, your application was not approved (AI Confidence: {aiResult.ConfidenceScore}%). Concerns: {redFlagsText}. You may reapply with improved credentials.";
                    }
                    else
                    {
                        // Manual review required
                        historyReason = $"Pending manual review (AI Score: {aiResult.ConfidenceScore}%). {aiResult.Reasoning}";
                    }
                }

                var history = new MentorApplicationHistory
                {
                    UserId = userId,
                    Action = historyAction,
                    ApplicationSnapshot = applicationSnapshot,
                    CertificationUrls = request.CertificationUrls,
                    Reason = historyReason,
                    AiConfidenceScore = aiResult?.ConfidenceScore,
                    AiModelVersion = aiResult?.ModelVersion,
                    SubmissionCount = submissionCount,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };
                await _unitOfWork.MentorApplicationHistories.AddAsync(history);

                // Create appropriate notification
                Notification notification;
                if (historyAction == ApplicationAction.AutoApproved)
                {
                    notification = new Notification
                    {
                        UserId = userId,
                        Type = NotificationType.System,
                        Title = "🎉 Mentor Application Approved!",
                        Body = "Congratulations! Your mentor application has been automatically approved. Please go to your Profile to confirm switching to Mentor role.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                }
                else if (historyAction == ApplicationAction.AutoRejected)
                {
                    notification = new Notification
                    {
                        UserId = userId,
                        Type = NotificationType.System,
                        Title = "Mentor Application Update",
                        Body = "Your mentor application has been reviewed. Please check your profile for details.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                }
                else
                {
                    notification = new Notification
                    {
                        UserId = userId,
                        Type = NotificationType.System,
                        Title = "Mentor Application Submitted",
                        Body = "Your mentor application has been submitted and is awaiting review.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                }
                await _unitOfWork.Notifications.AddAsync(notification);

                await _unitOfWork.SaveChangesAsync();
            });
            } // end try
            catch (Exception ex)
            {
                _logger.LogError(ex, "ApplyAsMentorAsync failed for userId={UserId}. Exception: {Message}", userId, ex.ToString());
                throw;
            }

            // Send email notification based on AI decision (outside transaction)
            if (aiResult != null && user.UserProfile != null)
            {
                var fullName = user.UserProfile.FullName ?? user.Email;
                try
                {
                    if (aiResult.RecommendedAction == "AUTO_APPROVE")
                    {
                        await _emailService.SendMentorApprovalNotificationAsync(user.Email, fullName);
                        _logger.LogInformation("Sent approval email to user {UserId}", userId);
                    }
                    else if (aiResult.RecommendedAction == "AUTO_REJECT")
                    {
                        var rejectReason = aiResult.RedFlags.Any()
                            ? string.Join("; ", aiResult.RedFlags)
                            : "Application did not meet minimum requirements";
                        await _emailService.SendMentorRejectionNotificationAsync(user.Email, fullName, rejectReason);
                        _logger.LogInformation("Sent rejection email to user {UserId}", userId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email notification to user {UserId}", userId);
                }
            }

            return ServiceResult.SuccessResult(resultMessage);
        }

        public async Task<ServiceResult<MentorApplicationStatusDto>> GetApplicationStatusAsync(long userId)
        {
            var mentorProfile = await _unitOfWork.MentorProfiles.GetByUserIdAsync(userId);

            if (mentorProfile == null)
            {
                return ServiceResult<MentorApplicationStatusDto>.SuccessResult(new MentorApplicationStatusDto
                {
                    HasApplied = false,
                    Status = null,
                    StatusText = "Not Applied"
                });
            }

            var statusText = mentorProfile.ApprovalStatus switch
            {
                MentorApprovalStatus.Pending => "Pending Review",
                MentorApprovalStatus.Approved => "Approved - Ready to Confirm",
                MentorApprovalStatus.Rejected => "Rejected",
                _ => "Unknown"
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

        public async Task<ServiceResult> ConfirmSwitchToMentorAsync(long userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return ServiceResult.NotFoundResult("User not found");

            if (user.Role != UserRole.Student)
                return ServiceResult.FailResult("Only students can confirm switch to mentor");

            var mentorProfile = await _unitOfWork.MentorProfiles.GetByUserIdAsync(userId);
            if (mentorProfile == null)
                return ServiceResult.NotFoundResult("No mentor application found");

            if (mentorProfile.ApprovalStatus != MentorApprovalStatus.Approved)
                return ServiceResult.FailResult("Your application has not been approved yet");

            // Change user role to Mentor
            user.Role = UserRole.Mentor;
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);

            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Congratulations! You are now a Mentor. Please log in again to access mentor features.");
        }

        #endregion
    }
}
