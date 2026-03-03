namespace MentorEdu.DTOs.Category
{
    /// <summary>
    /// Danh mục kèm hashtag đã ánh xạ (dạng nhóm)
    /// </summary>
    public class CategoryHashtagMappingDto
    {
        public long CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public List<CategoryHashtagItemDto> Hashtags { get; set; } = new();
    }

    /// <summary>
    /// Chi tiết danh mục kèm hashtag đã ánh xạ
    /// </summary>
    public class CategoryHashtagDetailDto
    {
        public long CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryDescription { get; set; }
        public List<CategoryHashtagItemDto> Hashtags { get; set; } = new();
    }

    /// <summary>
    /// DTO hashtag đơn giản dùng cho ánh xạ danh mục
    /// </summary>
    public class CategoryHashtagItemDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Yêu cầu thêm ánh xạ hashtag vào danh mục
    /// </summary>
    public class AddCategoryHashtagDto
    {
        public long CategoryId { get; set; }
        public long HashtagId { get; set; }
    }

    /// <summary>
    /// Yêu cầu xóa ánh xạ hashtag khỏi danh mục
    /// </summary>
    public class RemoveCategoryHashtagDto
    {
        public long CategoryId { get; set; }
        public long HashtagId { get; set; }
    }
}
