using MentorEdu.Models.Enums;

namespace MentorEdu.DTOs.User
{
    /// <summary>
    /// Thông tin c? b?n c?a User (dùng cho list, reference)
    /// </summary>
    public class UserBasicDto
    {
        public long Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public UserRole Role { get; set; }
        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Thông tin chi ti?t User (bao g?m profile)
    /// </summary>
    public class UserDetailDto
    {
        public long Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }

        // User Profile
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public string? School { get; set; }
        public string? Major { get; set; }
        public string? Bio { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }

        // Mentor Profile (n?u là mentor)
        public MentorProfileDto? MentorProfile { get; set; }
    }

    /// <summary>
    /// Thông tin Mentor Profile
    /// </summary>
    public class MentorProfileDto
    {
        public string? Title { get; set; }
        public decimal? HourlyRate { get; set; }
        public decimal? PackagePrice { get; set; }
        public int? ExperienceYears { get; set; }
        public string? Introduction { get; set; }
        public string? AvailabilityNote { get; set; }
        public decimal? RatingAvg { get; set; }
        public int? RatingCount { get; set; }
        public List<CategoryDto>? Categories { get; set; }
        public List<HashtagDto>? Hashtags { get; set; }
    }

    /// <summary>
    /// DTO hi?n th? Mentor trong danh sách
    /// </summary>
    public class MentorListDto
    {
        public long Id { get; set; }
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Title { get; set; }
        public decimal? HourlyRate { get; set; }
        public int? ExperienceYears { get; set; }
        public decimal? RatingAvg { get; set; }
        public int? RatingCount { get; set; }
        public string? School { get; set; }
        public string? City { get; set; }
        public List<string>? Categories { get; set; }
        public List<string>? Hashtags { get; set; }
    }

    /// <summary>
    /// Category DTO (dùng chung)
    /// </summary>
    public class CategoryDto
    {
        public long Id { get; set; }
        public string? Name { get; set; }
    }

    /// <summary>
    /// Hashtag DTO (dùng chung)
    /// </summary>
    public class HashtagDto
    {
        public long Id { get; set; }
        public string? Name { get; set; }
    }
}
