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
        [Required(ErrorMessage = "Specialization is required")]
        [MaxLength(500)]
        public string Specialization { get; set; } = string.Empty;

        /// <summary>
        /// Years of professional experience
        /// </summary>
        [Required(ErrorMessage = "Experience years is required")]
        [Range(0, 50, ErrorMessage = "Experience years must be between 0-50")]
        public int ExperienceYears { get; set; }

        /// <summary>
        /// Self introduction / bio
        /// </summary>
        [Required(ErrorMessage = "Introduction is required")]
        [MinLength(50, ErrorMessage = "Introduction must have at least 50 characters")]
        public string Introduction { get; set; } = string.Empty;

        /// <summary>
        /// Portfolio URL (GitHub, LinkedIn, personal website)
        /// </summary>
        [Required(ErrorMessage = "Portfolio URL (GitHub/LinkedIn) is required")]
        [MaxLength(500)]
        [Url(ErrorMessage = "Invalid Portfolio URL")]
        public string PortfolioUrl { get; set; } = string.Empty;

        /// <summary>
        /// Comma-separated URLs of uploaded certification files (from /api/file/upload/certifications)
        /// REQUIRED - Must upload at least 1 file
        /// </summary>
        [Required(ErrorMessage = "Certification is required - please upload at least 1 file")]
        public string CertificationUrls { get; set; } = string.Empty;

        /// <summary>
        /// Additional notes for the application (optional)
        /// </summary>
        public string? ApplicationNote { get; set; }

        /// <summary>
        /// List of category IDs that mentor specializes in (REQUIRED - at least 1)
        /// </summary>
        [Required(ErrorMessage = "Please select at least one category")]
        [MinLength(1, ErrorMessage = "Please select at least one category")]
        public List<long> CategoryIds { get; set; } = new();

        /// <summary>
        /// List of hashtag IDs for more specific expertise (optional)
        /// </summary>
        public List<long>? HashtagIds { get; set; }
    }
}
