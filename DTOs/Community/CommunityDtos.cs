using System.ComponentModel.DataAnnotations;

namespace MentorEdu.DTOs.Community
{
    /// <summary>
    /// Community Post List DTO
    /// </summary>
    public class CommunityPostListDto
    {
        public long Id { get; set; }
        public string? Title { get; set; }
        public string? ContentPreview { get; set; } // Rút g?n content
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string TimeAgo => GetTimeAgo();

        // Author
        public long AuthorId { get; set; }
        public string? AuthorName { get; set; }
        public string? AuthorAvatar { get; set; }

        // Stats
        public int CommentCount { get; set; }
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }

        private string GetTimeAgo()
        {
            if (!CreatedAt.HasValue) return "";

            var timeSpan = DateTime.UtcNow - CreatedAt.Value;

            if (timeSpan.TotalMinutes < 1) return "V?a xong";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} phút tr??c";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} gi? tr??c";
            if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays} ngày tr??c";

            return CreatedAt.Value.ToString("dd/MM/yyyy");
        }
    }

    /// <summary>
    /// Community Post Detail DTO
    /// </summary>
    public class CommunityPostDetailDto
    {
        public long Id { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Author
        public PostAuthorDto? Author { get; set; }

        // Stats
        public int CommentCount { get; set; }
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public bool IsLiked { get; set; } // User hi?n t?i ?ã like ch?a

        // Comments
        public List<CommunityCommentDto>? Comments { get; set; }
    }

    /// <summary>
    /// Post Author DTO
    /// </summary>
    public class PostAuthorDto
    {
        public long Id { get; set; }
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Title { get; set; } // Mentor title n?u có
        public bool IsMentor { get; set; }
    }

    /// <summary>
    /// Community Comment DTO
    /// </summary>
    public class CommunityCommentDto
    {
        public long Id { get; set; }
        public string? Content { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string TimeAgo => GetTimeAgo();

        // Author
        public long AuthorId { get; set; }
        public string? AuthorName { get; set; }
        public string? AuthorAvatar { get; set; }
        public bool IsAuthorMentor { get; set; }

        private string GetTimeAgo()
        {
            if (!CreatedAt.HasValue) return "";

            var timeSpan = DateTime.UtcNow - CreatedAt.Value;

            if (timeSpan.TotalMinutes < 1) return "V?a xong";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} phút tr??c";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} gi? tr??c";
            if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays} ngày tr??c";

            return CreatedAt.Value.ToString("dd/MM/yyyy");
        }
    }

    /// <summary>
    /// Request t?o Post
    /// </summary>
    public class CreatePostDto
    {
        [Required(ErrorMessage = "Tiêu ?? là b?t bu?c")]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "N?i dung là b?t bu?c")]
        public string Content { get; set; } = string.Empty;

        public List<string>? Tags { get; set; }
    }

    /// <summary>
    /// Request c?p nh?t Post
    /// </summary>
    public class UpdatePostDto
    {
        [Required(ErrorMessage = "Tiêu ?? là b?t bu?c")]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "N?i dung là b?t bu?c")]
        public string Content { get; set; } = string.Empty;

        public List<string>? Tags { get; set; }
    }

    /// <summary>
    /// Request t?o Comment
    /// </summary>
    public class CreateCommentDto
    {
        [Required]
        public long PostId { get; set; }

        [Required(ErrorMessage = "N?i dung bình lu?n là b?t bu?c")]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request tìm ki?m Post
    /// </summary>
    public class SearchPostDto
    {
        public string? Keyword { get; set; }
        public long? AuthorId { get; set; }
        public List<string>? Tags { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } = "createdAt"; // createdAt, comments, views, likes
        public bool SortDescending { get; set; } = true;
    }
}
