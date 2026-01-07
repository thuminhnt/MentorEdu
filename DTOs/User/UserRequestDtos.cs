using System.ComponentModel.DataAnnotations;

namespace MentorEdu.DTOs.User
{
    /// <summary>
    /// Request c?p nh?t User Profile
    /// </summary>
    public class UpdateUserProfileRequest
    {
        [MaxLength(255)]
        public string? FullName { get; set; }

        public string? AvatarUrl { get; set; }

        [MaxLength(30)]
        [Phone(ErrorMessage = "S? ?i?n tho?i không h?p l?")]
        public string? Phone { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }

        [MaxLength(255)]
        public string? School { get; set; }

        [MaxLength(255)]
        public string? Major { get; set; }

        public string? Bio { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }
    }

    /// <summary>
    /// Request c?p nh?t Mentor Profile
    /// </summary>
    public class UpdateMentorProfileRequest
    {
        [MaxLength(255)]
        public string? Title { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá theo gi? ph?i l?n h?n 0")]
        public decimal? HourlyRate { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá gói ph?i l?n h?n 0")]
        public decimal? PackagePrice { get; set; }

        [Range(0, 100, ErrorMessage = "S? n?m kinh nghi?m không h?p l?")]
        public int? ExperienceYears { get; set; }

        public string? Introduction { get; set; }

        public string? AvailabilityNote { get; set; }

        public List<long>? CategoryIds { get; set; }

        public List<long>? HashtagIds { get; set; }

        public List<string>? NewHashtags { get; set; }
    }

    /// <summary>
    /// Request tìm ki?m Mentor
    /// </summary>
    public class SearchMentorRequest
    {
        public string? Keyword { get; set; }
        public long? CategoryId { get; set; }
        public List<long>? HashtagIds { get; set; }
        public decimal? MinHourlyRate { get; set; }
        public decimal? MaxHourlyRate { get; set; }
        public int? MinExperienceYears { get; set; }
        public decimal? MinRating { get; set; }
        public string? City { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } = "rating"; // rating, price, experience
        public bool SortDescending { get; set; } = true;
    }
}
