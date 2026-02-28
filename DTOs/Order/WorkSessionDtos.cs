using System.ComponentModel.DataAnnotations;
using MentorEdu.Models.Enums;

namespace MentorEdu.DTOs.Order
{
    /// <summary>
    /// Work Session List DTO
    /// </summary>
    public class WorkSessionListDto
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? TotalMinutes { get; set; }
        public WorkSessionStatus Status { get; set; }
        public string StatusDisplay => Status.ToString();
        public DateTime? CreatedAt { get; set; }
    }

    /// <summary>
    /// Work Session Detail DTO
    /// </summary>
    public class WorkSessionDetailDto
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public long MentorId { get; set; }
        public string? MentorName { get; set; }
        public long StudentId { get; set; }
        public string? StudentName { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? TotalMinutes { get; set; }
        public WorkSessionStatus Status { get; set; }
        public string StatusDisplay => Status.ToString();
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Session Trackings (c�c l?n Start/Stop)
        public List<SessionTrackingDto>? Trackings { get; set; }
    }

    /// <summary>
    /// Session Tracking DTO
    /// </summary>
    public class SessionTrackingDto
    {
        public long Id { get; set; }
        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }
        public int? DurationMin { get; set; }
    }

    /// <summary>
    /// Request b?t ??u Work Session
    /// </summary>
    public class StartWorkSessionDto
    {
        [Required]
        public long OrderId { get; set; }

        public string? Note { get; set; }
    }

    /// <summary>
    /// Request k?t th�c/pause Work Session
    /// </summary>
    public class EndWorkSessionDto
    {
        [Required]
        public long SessionId { get; set; }

        public bool Pause { get; set; } = false;

        public string? Note { get; set; }
    }

    /// <summary>
    /// Request resume Work Session
    /// </summary>
    public class ResumeWorkSessionDto
    {
        [Required]
        public long SessionId { get; set; }
    }

    /// <summary>
    /// Work Session Summary cho Order
    /// </summary>
    public class WorkSessionSummaryDto
    {
        public long OrderId { get; set; }
        public string? OrderStatus { get; set; }
        public int TotalSessions { get; set; }
        public int TotalMinutes { get; set; }
        public decimal TotalHours => Math.Round(TotalMinutes / 60.0m, 2);
        public decimal? EstimatedCost { get; set; }
        public WorkSessionListDto? CurrentSession { get; set; }
    }
}
