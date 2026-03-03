using System.ComponentModel.DataAnnotations;

namespace MentorEdu.DTOs.Admin
{
    #region Hashtags Management

    /// <summary>Admin Hashtag DTO with usage count</summary>
    public class AdminHashtagDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MentorCount { get; set; }
        public int CategoryCount { get; set; }
    }

    /// <summary>Create Hashtag DTO</summary>
    public class CreateHashtagDto
    {
        [Required(ErrorMessage = "Name is required")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>Admin Search Hashtag DTO</summary>
    public class AdminSearchHashtagDto
    {
        public string? Keyword { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    #endregion
}
