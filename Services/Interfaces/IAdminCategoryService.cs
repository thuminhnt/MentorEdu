using MentorEdu.DTOs.Admin;
using MentorEdu.DTOs.Category;
using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.User;
using MentorEdu.Services.Common;

namespace MentorEdu.Services.Interfaces
{
    public interface IAdminCategoryService
    {
        // Danh mục
        Task<ServiceResult<List<CategoryResponseDto>>> GetCategoriesAsync();
        Task<ServiceResult<CategoryResponseDto>> GetCategoryByIdAsync(long categoryId);
        Task<ServiceResult<CategoryResponseDto>> CreateCategoryAsync(CreateCategoryRequest request);
        Task<ServiceResult<CategoryResponseDto>> UpdateCategoryAsync(long categoryId, UpdateCategoryRequest request);
        Task<ServiceResult> DeleteCategoryAsync(long categoryId);

        // Ánh xạ Danh mục - Hashtag
        Task<ServiceResult<List<CategoryHashtagMappingDto>>> GetCategoryHashtagMappingsAsync();
        Task<ServiceResult<CategoryHashtagDetailDto>> GetHashtagsByCategoryAsync(long categoryId);
        Task<ServiceResult> AddCategoryHashtagMappingAsync(AddCategoryHashtagDto request);
        Task<ServiceResult> RemoveCategoryHashtagMappingAsync(RemoveCategoryHashtagDto request);

        // Hashtag
        Task<ServiceResult<PagedResponse<AdminHashtagDto>>> GetAllHashtagsAsync(AdminSearchHashtagDto request);
        Task<ServiceResult<List<HashtagDto>>> GetHashtagsAsync();
        Task<ServiceResult<AdminHashtagDto>> CreateHashtagAsync(CreateHashtagDto request);
        Task<ServiceResult> DeleteHashtagAsync(long hashtagId);
    }
}
