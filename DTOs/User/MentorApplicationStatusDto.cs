using MentorEdu.Models.Enums;

namespace MentorEdu.DTOs.User
{
    /// <summary>
    /// DTO trạng thái đơn đăng ký mentor (hiển thị trong trang hồ sơ)
    /// </summary>
    public class MentorApplicationStatusDto
    {
        public bool HasApplied { get; set; }
        public MentorApprovalStatus? Status { get; set; }
        public string? StatusText { get; set; }
        public DateTime? AppliedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? RejectionReason { get; set; }
        
        // Chi tiết đơn đăng ký (nếu tồn tại)
        public string? Specialization { get; set; }
        public int? ExperienceYears { get; set; }
    }
}
