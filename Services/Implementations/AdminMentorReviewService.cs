using MentorEdu.DTOs.Admin;
using MentorEdu.DTOs.Common;
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
    public class AdminMentorReviewService : BaseService, IAdminMentorReviewService
    {
        private readonly IEmailService _emailService;
        private readonly IGeminiAiService _geminiAiService;
        private readonly ILogger<AdminMentorReviewService> _logger;

        public AdminMentorReviewService(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            IGeminiAiService geminiAiService,
            ILogger<AdminMentorReviewService> logger) : base(unitOfWork)
        {
            _emailService = emailService;
            _geminiAiService = geminiAiService;
            _logger = logger;
        }

        // ── Mentor Applications ───────────────────────────────────────────

        public async Task<ServiceResult<PagedResponse<MentorApplicationDto>>> GetMentorApplicationsAsync(
            int pageNumber = 1, int pageSize = 10, MentorApprovalStatus? status = null)
        {
            var query = _unitOfWork.MentorProfiles.Query();

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

            var dtos = applications.Select(mp => MapToMentorApplicationDto(mp));

            return ServiceResult<PagedResponse<MentorApplicationDto>>.SuccessResult(
                PagedResponse<MentorApplicationDto>.Create(dtos, pageNumber, pageSize, totalCount));
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

            var dto = MapToMentorApplicationDto(mentorProfile);

            // Bổ sung dữ liệu AI từ lịch sử xét duyệt
            var latestHistory = await _unitOfWork.MentorApplicationHistories.Query()
                .Where(h => h.UserId == userId && h.AiConfidenceScore != null)
                .OrderByDescending(h => h.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestHistory != null)
            {
                dto.AiConfidenceScore = latestHistory.AiConfidenceScore;
                dto.AiModelVersion = latestHistory.AiModelVersion;
                dto.IsAiReviewed = true;

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
                    catch (JsonException) { /* Ignore JSON parsing errors */ }
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
            if (user == null) return ServiceResult.NotFoundResult("User not found");

            var submissionCount = await _unitOfWork.MentorApplicationHistories.GetSubmissionCountAsync(userId);

            return await _unitOfWork.ExecuteWithStrategyAsync(async () =>
            {
                mentorProfile.ApprovalStatus = MentorApprovalStatus.Approved;
                mentorProfile.ReviewedAt = DateTime.UtcNow;
                mentorProfile.ReviewedBy = adminId;
                mentorProfile.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.MentorProfiles.Update(mentorProfile);

                var applicationSnapshot = JsonSerializer.Serialize(new
                {
                    mentorProfile.Specialization,
                    mentorProfile.ExperienceYears,
                    mentorProfile.Introduction,
                    mentorProfile.PortfolioUrl,
                    mentorProfile.ApplicationNote,
                    mentorProfile.Title
                });

                await _unitOfWork.MentorApplicationHistories.AddAsync(new MentorApplicationHistory
                {
                    UserId = userId,
                    Action = ApplicationAction.Approved,
                    ApplicationSnapshot = applicationSnapshot,
                    CertificationUrls = mentorProfile.CertificationUrls,
                    SubmissionCount = submissionCount > 0 ? submissionCount : 1,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = adminId
                });

                await _unitOfWork.Notifications.AddAsync(new Notification
                {
                    UserId = userId,
                    Type = NotificationType.System,
                    Title = "Mentor Application Approved",
                    Body = "Congratulations! Your mentor application has been approved. Please go to your Profile to confirm switching to Mentor role.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                var fullName = user.UserProfile?.FullName ?? user.Email;
                try { await _emailService.SendMentorApprovalNotificationAsync(user.Email, fullName); }
                catch { /* Don't fail if email fails */ }

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

            var user = await _unitOfWork.Users.GetByIdWithProfilesAsync(userId);
            if (user == null) return ServiceResult.NotFoundResult("User not found");

            var fullName = user.UserProfile?.FullName ?? user.Email.Split('@')[0];
            var submissionCount = await _unitOfWork.MentorApplicationHistories.GetSubmissionCountAsync(userId);

            mentorProfile.ApprovalStatus = MentorApprovalStatus.Rejected;
            mentorProfile.ReviewedAt = DateTime.UtcNow;
            mentorProfile.ReviewedBy = adminId;
            mentorProfile.RejectionReason = reason;
            mentorProfile.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.MentorProfiles.Update(mentorProfile);

            var applicationSnapshot = JsonSerializer.Serialize(new
            {
                mentorProfile.Specialization,
                mentorProfile.ExperienceYears,
                mentorProfile.Introduction,
                mentorProfile.PortfolioUrl,
                mentorProfile.ApplicationNote,
                mentorProfile.Title
            });

            await _unitOfWork.MentorApplicationHistories.AddAsync(new MentorApplicationHistory
            {
                UserId = userId,
                Action = ApplicationAction.Rejected,
                ApplicationSnapshot = applicationSnapshot,
                CertificationUrls = mentorProfile.CertificationUrls,
                Reason = reason,
                SubmissionCount = submissionCount > 0 ? submissionCount : 1,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = adminId
            });

            await _unitOfWork.Notifications.AddAsync(new Notification
            {
                UserId = userId,
                Type = NotificationType.System,
                Title = "Mentor Application Rejected",
                Body = $"Your mentor application has been rejected. Reason: {reason}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();

            try { await _emailService.SendMentorRejectionNotificationAsync(user.Email, fullName, reason); }
            catch (Exception ex) { _logger.LogError(ex, "Failed to send rejection email for user {UserId}", userId); }

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

            var submissionCount = await _unitOfWork.MentorApplicationHistories.GetSubmissionCountAsync(userId);
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

                await _unitOfWork.Notifications.AddAsync(new Notification
                {
                    UserId = userId,
                    Type = NotificationType.System,
                    Title = "Mentor Application Approved",
                    Body = $"Your mentor application has been approved by AI review (Score: {aiResult.ConfidenceScore}%). Go to Profile to confirm switching to Mentor role.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
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

                await _unitOfWork.Notifications.AddAsync(new Notification
                {
                    UserId = userId,
                    Type = NotificationType.System,
                    Title = "Mentor Application Rejected",
                    Body = $"Your mentor application was rejected by AI review. Concerns: {redFlagsText}. You may reapply.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                historyReason = $"AI re-evaluation: Still pending manual review (Score: {aiResult.ConfidenceScore}%). {aiResult.Reasoning}";
                resultMessage = $"AI scored the application at {aiResult.ConfidenceScore}%. Still requires manual review.";
            }

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

            await _unitOfWork.MentorApplicationHistories.AddAsync(new MentorApplicationHistory
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
            });

            await _unitOfWork.SaveChangesAsync();

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

            var detailResult = await GetMentorApplicationDetailAsync(userId);
            if (detailResult.Success)
                return ServiceResult<MentorApplicationDto>.SuccessResult(detailResult.Data!, resultMessage);

            return ServiceResult<MentorApplicationDto>.SuccessResult(null!, resultMessage);
        }

        // ── Application History ───────────────────────────────────────────

        public async Task<ServiceResult<PagedResponse<MentorApplicationHistoryDto>>> GetApplicationHistoryAsync(
            ApplicationHistoryQueryParams queryParams)
        {
            var historyItems = await _unitOfWork.MentorApplicationHistories.GetAllAsync();
            var query = historyItems.AsQueryable();

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

            var userIds = items.Select(h => h.UserId).Distinct().ToList();
            var creatorIds = items.Where(h => h.CreatedBy.HasValue).Select(h => h.CreatedBy!.Value).Distinct().ToList();
            var allUserIds = userIds.Union(creatorIds).ToList();

            var users = new Dictionary<long, User>();
            foreach (var uid in allUserIds)
            {
                var user = await _unitOfWork.Users.GetByIdWithProfilesAsync(uid);
                if (user != null) users[uid] = user;
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
                CreatedByEmail = h.CreatedBy.HasValue && users.ContainsKey(h.CreatedBy.Value)
                    ? users[h.CreatedBy.Value].Email : null
            }).ToList();

            return ServiceResult<PagedResponse<MentorApplicationHistoryDto>>.SuccessResult(
                PagedResponse<MentorApplicationHistoryDto>.Create(dtoList, queryParams.PageNumber, queryParams.PageSize, totalCount));
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
                if (creator != null) creators[cid] = creator;
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
                CreatedByEmail = h.CreatedBy.HasValue && creators.ContainsKey(h.CreatedBy.Value)
                    ? creators[h.CreatedBy.Value].Email : null
            }).ToList();

            return ServiceResult<List<MentorApplicationHistoryDto>>.SuccessResult(dtoList);
        }

        public async Task<ServiceResult<List<MentorApplicationHistoryExportDto>>> ExportApplicationHistoryForMLAsync(
            DateTime? fromDate = null, DateTime? toDate = null, ApplicationAction? action = null)
        {
            var historyItems = await _unitOfWork.MentorApplicationHistories.GetAllAsync();
            var query = historyItems.AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(h => h.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(h => h.CreatedAt <= toDate.Value);

            if (action.HasValue)
                query = query.Where(h => h.Action == action.Value);

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
                    IsManualReview = h.CreatedBy.HasValue,
                    AiConfidenceScore = h.AiConfidenceScore
                })
                .ToList();

            return ServiceResult<List<MentorApplicationHistoryExportDto>>.SuccessResult(exportList);
        }

        // ── Private helpers ───────────────────────────────────────────────

        private static MentorApplicationDto MapToMentorApplicationDto(MentorProfile mp) => new()
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
        };
    }
}
