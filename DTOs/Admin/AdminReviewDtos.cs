namespace MentorEdu.DTOs.Admin
{
    #region Reviews Management

    /// <summary>Admin Review List DTO</summary>
    public class AdminReviewListDto
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public string? OrderTitle { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Từ người dùng
        public long FromUserId { get; set; }
        public string? FromUserName { get; set; }
        public string? FromUserEmail { get; set; }

        // Đến người dùng
        public long ToUserId { get; set; }
        public string? ToUserName { get; set; }
        public string? ToUserEmail { get; set; }
    }

    /// <summary>Admin Search Review DTO</summary>
    public class AdminSearchReviewDto
    {
        public long? OrderId { get; set; }
        public long? FromUserId { get; set; }
        public long? ToUserId { get; set; }
        public int? MinRating { get; set; }
        public int? MaxRating { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    #endregion
}
