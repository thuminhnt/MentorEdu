namespace MentorEdu.DTOs.Category
{
    /// <summary>
    /// Represents a category with its mapped hashtags (grouped view)
    /// </summary>
    public class CategoryHashtagMappingDto
    {
        public long CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public List<CategoryHashtagItemDto> Hashtags { get; set; } = new();
    }

    /// <summary>
    /// Detail of a category with its mapped hashtags
    /// </summary>
    public class CategoryHashtagDetailDto
    {
        public long CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryDescription { get; set; }
        public List<CategoryHashtagItemDto> Hashtags { get; set; } = new();
    }

    /// <summary>
    /// Simple hashtag DTO for category mapping
    /// </summary>
    public class CategoryHashtagItemDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to add a hashtag mapping to a category
    /// </summary>
    public class AddCategoryHashtagDto
    {
        public long CategoryId { get; set; }
        public long HashtagId { get; set; }
    }

    /// <summary>
    /// Request to remove a hashtag mapping from a category
    /// </summary>
    public class RemoveCategoryHashtagDto
    {
        public long CategoryId { get; set; }
        public long HashtagId { get; set; }
    }
}
