using System.ComponentModel.DataAnnotations;

namespace MentorEdu.DTOs.Hashtag
{
    /// <summary>
    /// Hashtag Response DTO
    /// </summary>
    public class HashtagResponseDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MentorCount { get; set; }
    }

    /// <summary>
    /// Request t?o Hashtag
    /// </summary>
    public class CreateHashtagRequest
    {
        [Required(ErrorMessage = "Tên hashtag là b?t bu?c")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request tìm ki?m Hashtag
    /// </summary>
    public class SearchHashtagRequest
    {
        public string? Keyword { get; set; }
        public int Limit { get; set; } = 20;
    }
}
