using System.ComponentModel.DataAnnotations;

namespace MentorEdu.DTOs.User
{
    /// <summary>
    /// Yêu cầu đăng ký trở thành mentor (từ trang hồ sơ)
    /// Tất cả các trường đều BẮT BUỘC
    /// </summary>
    public class ApplyAsMentorRequest
    {
        /// <summary>
        /// Chuyên ngành của mentor
        /// </summary>
        [Required(ErrorMessage = "Specialization is required")]
        [MaxLength(500)]
        public string Specialization { get; set; } = string.Empty;

        /// <summary>
        /// Số năm kinh nghiệm chuyên môn
        /// </summary>
        [Required(ErrorMessage = "Experience years is required")]
        [Range(0, 50, ErrorMessage = "Experience years must be between 0-50")]
        public int ExperienceYears { get; set; }

        /// <summary>
        /// Giới thiệu bản thân / tiểu sử
        /// </summary>
        [Required(ErrorMessage = "Introduction is required")]
        [MinLength(50, ErrorMessage = "Introduction must have at least 50 characters")]
        public string Introduction { get; set; } = string.Empty;

        /// <summary>
        /// URL Portfolio (GitHub, LinkedIn, website cá nhân)
        /// </summary>
        [Required(ErrorMessage = "Portfolio URL (GitHub/LinkedIn) is required")]
        [MaxLength(500)]
        [Url(ErrorMessage = "Invalid Portfolio URL")]
        public string PortfolioUrl { get; set; } = string.Empty;

        /// <summary>
        /// URL các file chứng chỉ đã tải lên, phân cách bằng dấu phẩy (từ /api/file/upload/certifications)
        /// BẮT BUỘC - Phải tải lên ít nhất 1 file
        /// </summary>
        [Required(ErrorMessage = "Certification is required - please upload at least 1 file")]
        public string CertificationUrls { get; set; } = string.Empty;

        /// <summary>
        /// Ghi chú thêm của đơn đăng ký (ùy chọn)
        /// </summary>
        public string? ApplicationNote { get; set; }

        /// <summary>
        /// Danh sách ID danh mục mà mentor chuyên về (BẮT BUỘC - ít nhất 1)
        /// </summary>
        [Required(ErrorMessage = "Please select at least one category")]
        [MinLength(1, ErrorMessage = "Please select at least one category")]
        public List<long> CategoryIds { get; set; } = new();

        /// <summary>
        /// Danh sách ID hashtag cho chuyên môn cụ thể hơn (ùy chọn)
        /// </summary>
        public List<long>? HashtagIds { get; set; }
    }
}
