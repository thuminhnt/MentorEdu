using System.ComponentModel.DataAnnotations;

namespace MentorEdu.DTOs.User
{
    /// <summary>
    /// Request to apply as mentor (from Profile page)
    /// All fields are REQUIRED
    /// </summary>
    public class ApplyAsMentorRequest
    {
        /// <summary>
        /// Mentor's area of expertise
        /// </summary>
        [Required(ErrorMessage = "Chuyên môn là bắt buộc")]
        [MaxLength(500)]
        public string Specialization { get; set; } = string.Empty;

        /// <summary>
        /// Years of professional experience
        /// </summary>
        [Required(ErrorMessage = "Số năm kinh nghiệm là bắt buộc")]
        [Range(0, 50, ErrorMessage = "Số năm kinh nghiệm phải từ 0-50")]
        public int ExperienceYears { get; set; }

        /// <summary>
        /// Self introduction / bio
        /// </summary>
        [Required(ErrorMessage = "Giới thiệu bản thân là bắt buộc")]
        [MinLength(50, ErrorMessage = "Giới thiệu phải có ít nhất 50 ký tự")]
        public string Introduction { get; set; } = string.Empty;

        /// <summary>
        /// Portfolio URL (GitHub, LinkedIn, personal website)
        /// </summary>
        [Required(ErrorMessage = "Link Portfolio (GitHub/LinkedIn) là bắt buộc")]
        [MaxLength(500)]
        [Url(ErrorMessage = "Link Portfolio không hợp lệ")]
        public string PortfolioUrl { get; set; } = string.Empty;

        /// <summary>
        /// Comma-separated URLs of uploaded certification files (from /api/file/upload/certifications)
        /// REQUIRED - Must upload at least 1 file
        /// </summary>
        [Required(ErrorMessage = "Chứng chỉ là bắt buộc - vui lòng upload ít nhất 1 file")]
        public string CertificationUrls { get; set; } = string.Empty;

        /// <summary>
        /// Additional notes for the application (optional)
        /// </summary>
        public string? ApplicationNote { get; set; }
    }
}
