using System.ComponentModel.DataAnnotations;

namespace MentorEdu.DTOs.Report
{
    /// <summary>
    /// Report DTO
    /// </summary>
    public class ReportDto
    {
        public long Id { get; set; }
        public string? Reason { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Reporter
        public long ReporterId { get; set; }
        public string? ReporterName { get; set; }

        // Reported User
        public long ReportedUserId { get; set; }
        public string? ReportedUserName { get; set; }

        // Order (n?u có)
        public long? OrderId { get; set; }
        public string? OrderTitle { get; set; }

        // Handler (admin x? lý)
        public long? HandledBy { get; set; }
        public string? HandlerName { get; set; }
    }

    /// <summary>
    /// Report Detail DTO (Admin view)
    /// </summary>
    public class ReportDetailDto
    {
        public long Id { get; set; }
        public string? Reason { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Reporter detail
        public ReportUserDto? Reporter { get; set; }

        // Reported User detail
        public ReportUserDto? ReportedUser { get; set; }

        // Order detail
        public ReportOrderDto? Order { get; set; }

        // Handler
        public string? HandlerName { get; set; }
        public string? HandlerNote { get; set; }
    }

    /// <summary>
    /// User info trong Report
    /// </summary>
    public class ReportUserDto
    {
        public long Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }
        public int TotalReports { get; set; } // S? l?n b? báo cáo
    }

    /// <summary>
    /// Order info trong Report
    /// </summary>
    public class ReportOrderDto
    {
        public long Id { get; set; }
        public string? Title { get; set; }
        public string? Status { get; set; }
        public decimal? TotalPrice { get; set; }
    }

    /// <summary>
    /// Request t?o Report
    /// </summary>
    public class CreateReportDto
    {
        [Required]
        public long ReportedUserId { get; set; }

        public long? OrderId { get; set; }

        [Required(ErrorMessage = "Lý do báo cáo là b?t bu?c")]
        [MaxLength(2000)]
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request x? lý Report (Admin)
    /// </summary>
    public class HandleReportDto
    {
        [Required]
        public long ReportId { get; set; }

        [Required(ErrorMessage = "Tr?ng thái là b?t bu?c")]
        public string Status { get; set; } = string.Empty; // resolved, rejected, warning, banned

        public string? Note { get; set; }

        public bool BanUser { get; set; } = false;
    }
}
