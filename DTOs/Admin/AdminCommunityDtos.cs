namespace MentorEdu.DTOs.Admin
{
    #region Community Management

    /// <summary>Admin Post List DTO</summary>
    public class AdminPostListDto
    {
        public long Id { get; set; }
        public string? Title { get; set; }
        public string? ContentPreview { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Tác giả
        public long AuthorId { get; set; }
        public string? AuthorName { get; set; }
        public string? AuthorEmail { get; set; }
        public string? AuthorAvatar { get; set; }

        // Thống kê
        public int CommentCount { get; set; }
        public int ReportCount { get; set; }
        public bool IsHidden { get; set; }
    }

    /// <summary>Admin Search Post DTO</summary>
    public class AdminSearchPostDto
    {
        public string? Keyword { get; set; }
        public long? AuthorId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool? IsHidden { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    /// <summary>Admin Comment List DTO</summary>
    public class AdminCommentListDto
    {
        public long Id { get; set; }
        public long PostId { get; set; }
        public string? PostTitle { get; set; }
        public string? Content { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Tác giả
        public long AuthorId { get; set; }
        public string? AuthorName { get; set; }
        public string? AuthorEmail { get; set; }
    }

    /// <summary>Admin Search Comment DTO</summary>
    public class AdminSearchCommentDto
    {
        public long? PostId { get; set; }
        public long? AuthorId { get; set; }
        public string? Keyword { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    #endregion
}
