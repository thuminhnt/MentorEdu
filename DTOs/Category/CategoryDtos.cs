using System.ComponentModel.DataAnnotations;

namespace MentorEdu.DTOs.Category
{
    /// <summary>
    /// Category Response DTO
    /// </summary>
    public class CategoryResponseDto
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public long? ParentId { get; set; }
        public string? ParentName { get; set; }
        public List<CategoryResponseDto>? Children { get; set; }
    }

    /// <summary>
    /// Category Tree DTO (hi?n th? d?ng cây)
    /// </summary>
    public class CategoryTreeDto
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public List<CategoryTreeDto> Children { get; set; } = new();
    }

    /// <summary>
    /// Request t?o Category
    /// </summary>
    public class CreateCategoryRequest
    {
        [Required(ErrorMessage = "Tên danh m?c là b?t bu?c")]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public long? ParentId { get; set; }
    }

    /// <summary>
    /// Request c?p nh?t Category
    /// </summary>
    public class UpdateCategoryRequest
    {
        [Required(ErrorMessage = "Tên danh m?c là b?t bu?c")]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public long? ParentId { get; set; }
    }
}
