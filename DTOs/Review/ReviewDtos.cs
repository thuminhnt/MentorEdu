using System.ComponentModel.DataAnnotations;

namespace MentorEdu.DTOs.Review
{
    /// <summary>
    /// Review DTO
    /// </summary>
    public class ReviewDto
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime? CreatedAt { get; set; }

        // From User
        public long FromUserId { get; set; }
        public string? FromUserName { get; set; }
        public string? FromUserAvatar { get; set; }

        // To User
        public long ToUserId { get; set; }
        public string? ToUserName { get; set; }
    }

    /// <summary>
    /// Review List cho Mentor Profile
    /// </summary>
    public class MentorReviewDto
    {
        public long Id { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Student (ng??i ?ánh giá)
        public string? StudentName { get; set; }
        public string? StudentAvatar { get; set; }

        // Order info
        public string? OrderTitle { get; set; }
    }

    /// <summary>
    /// Review Summary
    /// </summary>
    public class ReviewSummaryDto
    {
        public long UserId { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int FiveStarCount { get; set; }
        public int FourStarCount { get; set; }
        public int ThreeStarCount { get; set; }
        public int TwoStarCount { get; set; }
        public int OneStarCount { get; set; }

        // Percentage
        public double FiveStarPercent => TotalReviews > 0 ? (double)FiveStarCount / TotalReviews * 100 : 0;
        public double FourStarPercent => TotalReviews > 0 ? (double)FourStarCount / TotalReviews * 100 : 0;
        public double ThreeStarPercent => TotalReviews > 0 ? (double)ThreeStarCount / TotalReviews * 100 : 0;
        public double TwoStarPercent => TotalReviews > 0 ? (double)TwoStarCount / TotalReviews * 100 : 0;
        public double OneStarPercent => TotalReviews > 0 ? (double)OneStarCount / TotalReviews * 100 : 0;
    }

    /// <summary>
    /// Request t?o Review
    /// </summary>
    public class CreateReviewDto
    {
        [Required]
        public long OrderId { get; set; }

        [Required(ErrorMessage = "Rating là b?t bu?c")]
        [Range(1, 5, ErrorMessage = "Rating t? 1 ??n 5")]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }

    /// <summary>
    /// Request filter reviews
    /// </summary>
    public class SearchReviewDto
    {
        public int? Rating { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } = "createdAt";
        public bool SortDescending { get; set; } = true;
    }
}
