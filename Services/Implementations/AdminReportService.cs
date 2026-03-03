using MentorEdu.DTOs.Admin;
using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Report;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using MentorEdu.Services.Common;
using MentorEdu.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Services.Implementations
{
    public class AdminReportService : BaseService, IAdminReportService
    {
        private readonly INotificationService _notificationService;

        public AdminReportService(IUnitOfWork unitOfWork, INotificationService notificationService) : base(unitOfWork)
        {
            _notificationService = notificationService;
        }

        // ── Reports ───────────────────────────────────────────────────────

        public async Task<ServiceResult<PagedResponse<ReportDto>>> GetReportsAsync(
            int pageNumber, int pageSize, string? status = null)
        {
            var query = _unitOfWork.Reports.Query();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(r => r.Status == status);

            var totalCount = await query.CountAsync();

            var reports = await query
                .Include(r => r.Reporter).ThenInclude(u => u.UserProfile)
                .Include(r => r.ReportedUser).ThenInclude(u => u.UserProfile)
                .Include(r => r.Order)
                .Include(r => r.Review)
                .Include(r => r.Handler)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = new List<ReportDto>();
            foreach (var r in reports)
            {
                var dto = new ReportDto
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
                    ReviewId = r.ReviewId,
                    ReviewRating = r.Review?.Rating,
                    ReviewComment = r.Review?.Comment,
                    HandledBy = r.HandledBy,
                    HandlerName = r.Handler?.UserProfile?.FullName
                };

                if (r.ReviewId.HasValue && r.Review != null)
                {
                    dto.ReportedContentType = "Review";
                    dto.ReportedContentPreview = $"Rating: {r.Review.Rating}/5 - {(r.Review.Comment?.Length > 100 ? r.Review.Comment[..100] + "..." : r.Review.Comment)}";
                }
                else if (r.Details != null && (r.Details.Contains("[Community Post") || r.Details.Contains("Post id=") || r.Details.Contains("Post id =")))
                {
                    dto.ReportedContentType = "Post";
                    var postInfo = ParsePostInfoFromDetails(r.Details);
                    if (postInfo.HasValue)
                    {
                        var post = await _unitOfWork.CommunityPosts.GetByIdAsync(postInfo.Value.PostId);
                        dto.ReportedContentPreview = post != null
                            ? $"Post: \"{post.Title}\" - {(post.Content?.Length > 100 ? post.Content[..100] + "..." : post.Content)}"
                            : $"Post #{postInfo.Value.PostId}: {postInfo.Value.Title}";
                    }
                }
                else if (r.OrderId.HasValue)
                {
                    dto.ReportedContentType = "Order";
                    dto.ReportedContentPreview = $"Order #{r.OrderId}";
                }
                else
                {
                    dto.ReportedContentType = "User";
                    dto.ReportedContentPreview = r.Details;
                }

                dtos.Add(dto);
            }

            return ServiceResult<PagedResponse<ReportDto>>.SuccessResult(
                PagedResponse<ReportDto>.Create(dtos, pageNumber, pageSize, totalCount));
        }

        public async Task<ServiceResult<ReportDetailDto>> GetReportDetailAsync(long reportId)
        {
            var report = await _unitOfWork.Reports.Query()
                .Include(r => r.Reporter).ThenInclude(u => u.UserProfile)
                .Include(r => r.ReportedUser).ThenInclude(u => u.UserProfile)
                .Include(r => r.Order).ThenInclude(o => o!.Request)
                .Include(r => r.Review).ThenInclude(rv => rv!.FromUser).ThenInclude(u => u.UserProfile)
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

            if (report.ReviewId.HasValue && report.Review != null)
            {
                dto.ReportedContentType = "Review";
                dto.Review = new ReportReviewDto
                {
                    Id = report.Review.Id,
                    Rating = report.Review.Rating,
                    Comment = report.Review.Comment,
                    FromUserName = report.Review.FromUser?.UserProfile?.FullName,
                    ToUserName = report.ReportedUser?.UserProfile?.FullName,
                    CreatedAt = report.Review.CreatedAt
                };
            }
            else if (report.Details != null && (report.Details.Contains("[Community Post") || report.Details.Contains("Post id=") || report.Details.Contains("Post id =")))
            {
                dto.ReportedContentType = "Post";
                var postInfo = ParsePostInfoFromDetails(report.Details);
                if (postInfo.HasValue)
                {
                    var post = await _unitOfWork.CommunityPosts.Query()
                        .Include(p => p.Author).ThenInclude(a => a.UserProfile)
                        .FirstOrDefaultAsync(p => p.Id == postInfo.Value.PostId);
                    if (post != null)
                    {
                        dto.Post = new ReportPostDto
                        {
                            Id = post.Id,
                            Title = post.Title,
                            ContentPreview = post.Content?.Length > 300 ? post.Content[..300] + "..." : post.Content,
                            AuthorName = post.Author?.UserProfile?.FullName,
                            IsHidden = post.IsHidden,
                            CreatedAt = post.CreatedAt
                        };
                    }
                }
            }
            else if (report.OrderId.HasValue)
            {
                dto.ReportedContentType = "Order";
            }
            else
            {
                dto.ReportedContentType = "User";
            }

            return ServiceResult<ReportDetailDto>.SuccessResult(dto);
        }

        public async Task<ServiceResult> HandleReportAsync(long adminId, HandleReportDto request)
        {
            var report = await _unitOfWork.Reports.GetByIdAsync(request.ReportId);
            if (report == null) return ServiceResult.NotFoundResult("Report not found");

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

            // Nếu giải quyết báo cáo có liên quan đến đánh giá → xóa đánh giá đó
            if (request.Status.Equals("Resolved", StringComparison.OrdinalIgnoreCase) && report.ReviewId.HasValue)
            {
                var reviewId = report.ReviewId.Value;
                var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
                if (review != null)
                {
                    var reportsWithReview = await _unitOfWork.Reports.Query()
                        .Where(r => r.ReviewId == reviewId).ToListAsync();
                    foreach (var rpt in reportsWithReview)
                    {
                        rpt.ReviewId = null;
                        _unitOfWork.Reports.Update(rpt);
                    }

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

            // Nếu giải quyết báo cáo về bài viết → tự động ẩn bài và thông báo tác giả
            if (request.Status.Equals("Resolved", StringComparison.OrdinalIgnoreCase)
                && report.Details != null
                && (report.Details.Contains("[Community Post") || report.Details.Contains("Post id=") || report.Details.Contains("Post id =")))
            {
                var postInfo = ParsePostInfoFromDetails(report.Details);
                if (postInfo.HasValue)
                {
                    var post = await _unitOfWork.CommunityPosts.GetByIdAsync(postInfo.Value.PostId);
                    if (post != null && !post.IsHidden)
                    {
                        post.IsHidden = true;
                        post.HiddenReason = report.Reason ?? "Violated policies";
                        post.UpdatedAt = DateTime.UtcNow;
                        _unitOfWork.CommunityPosts.Update(post);
                        await _unitOfWork.SaveChangesAsync();

                        try
                        {
                            await _notificationService.CreateNotificationAsync(
                                post.AuthorId,
                                NotificationType.PostHidden,
                                "Post Hidden",
                                $"Your post \"{post.Title}\" has been hidden due to a report. Reason: {report.Reason ?? "Violated policies"}",
                                $"/community"
                            );
                        }
                        catch { /* Don't fail if notification fails */ }
                    }
                }
            }

            return ServiceResult.SuccessResult("Report handled successfully");
        }

        // ── Community Posts ───────────────────────────────────────────────

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

            return ServiceResult<PagedResponse<AdminPostListDto>>.SuccessResult(
                PagedResponse<AdminPostListDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount));
        }

        public async Task<ServiceResult> AdminDeletePostAsync(long postId, string reason)
        {
            var post = await _unitOfWork.CommunityPosts.Query()
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null) return ServiceResult.NotFoundResult("Post not found");

            if (post.Comments?.Any() == true)
                foreach (var comment in post.Comments)
                    _unitOfWork.CommunityComments.Remove(comment);

            _unitOfWork.CommunityPosts.Remove(post);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Post deleted successfully");
        }

        public async Task<ServiceResult> AdminHidePostAsync(long postId, string reason)
        {
            var post = await _unitOfWork.CommunityPosts.GetByIdAsync(postId);
            if (post == null) return ServiceResult.NotFoundResult("Post not found");

            post.IsHidden = true;
            post.HiddenReason = reason;
            post.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.CommunityPosts.Update(post);
            await _unitOfWork.SaveChangesAsync();

            try
            {
                await _notificationService.CreateNotificationAsync(
                    post.AuthorId,
                    NotificationType.PostHidden,
                    "Post Hidden",
                    $"Your post \"{post.Title}\" has been hidden by admin. Reason: {reason}",
                    $"/community"
                );
            }
            catch { /* Don't fail hide operation if notification fails */ }

            return ServiceResult.SuccessResult("Post hidden successfully");
        }

        public async Task<ServiceResult> AdminUnhidePostAsync(long postId)
        {
            var post = await _unitOfWork.CommunityPosts.GetByIdAsync(postId);
            if (post == null) return ServiceResult.NotFoundResult("Post not found");

            post.IsHidden = false;
            post.HiddenReason = null;
            post.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.CommunityPosts.Update(post);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Post unhidden successfully");
        }

        // ── Community Comments ────────────────────────────────────────────

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

            return ServiceResult<PagedResponse<AdminCommentListDto>>.SuccessResult(
                PagedResponse<AdminCommentListDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount));
        }

        public async Task<ServiceResult> AdminDeleteCommentAsync(long commentId, string reason)
        {
            var comment = await _unitOfWork.CommunityComments.GetByIdAsync(commentId);
            if (comment == null) return ServiceResult.NotFoundResult("Comment not found");

            _unitOfWork.CommunityComments.Remove(comment);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Comment deleted successfully");
        }

        // ── Private helpers ───────────────────────────────────────────────

        private static (long PostId, string Title)? ParsePostInfoFromDetails(string details)
        {
            // Định dạng: [Community Post #8: "tiêu đề"] ...
            var match = System.Text.RegularExpressions.Regex.Match(details,
                @"\[Community Post #(\d+):\s*""([^""]*)""\]");
            if (match.Success && long.TryParse(match.Groups[1].Value, out var postId1))
                return (postId1, match.Groups[2].Value);

            // Định dạng cũ: "Post id=8" hoặc "Post id = 8"
            var match2 = System.Text.RegularExpressions.Regex.Match(details,
                @"Post id\s*=\s*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match2.Success && long.TryParse(match2.Groups[1].Value, out var postId2))
                return (postId2, string.Empty);

            return null;
        }
    }
}
