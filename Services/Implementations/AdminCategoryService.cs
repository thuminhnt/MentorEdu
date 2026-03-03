using MentorEdu.DTOs.Admin;
using MentorEdu.DTOs.Category;
using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.User;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using MentorEdu.Services.Common;
using MentorEdu.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Services.Implementations
{
    public class AdminCategoryService : BaseService, IAdminCategoryService
    {
        public AdminCategoryService(IUnitOfWork unitOfWork) : base(unitOfWork) { }

        // ── Categories ─────────────────────────────────────────────────────

        public async Task<ServiceResult<List<CategoryResponseDto>>> GetCategoriesAsync()
        {
            var categories = await _unitOfWork.Categories.GetAllWithHierarchyAsync();
            var dtos = categories.Select(c => MapToCategoryDto(c)).ToList();
            return ServiceResult<List<CategoryResponseDto>>.SuccessResult(dtos);
        }

        public async Task<ServiceResult<CategoryResponseDto>> GetCategoryByIdAsync(long categoryId)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(categoryId);
            if (category == null)
                return ServiceResult<CategoryResponseDto>.NotFoundResult("Category not found");

            return ServiceResult<CategoryResponseDto>.SuccessResult(MapToCategoryDto(category));
        }

        public async Task<ServiceResult<CategoryResponseDto>> CreateCategoryAsync(CreateCategoryRequest request)
        {
            var category = new Category
            {
                Name = request.Name,
                Description = request.Description
            };

            await _unitOfWork.Categories.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<CategoryResponseDto>.CreatedResult(MapToCategoryDto(category), "Category created successfully");
        }

        public async Task<ServiceResult<CategoryResponseDto>> UpdateCategoryAsync(long categoryId, UpdateCategoryRequest request)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(categoryId);
            if (category == null)
                return ServiceResult<CategoryResponseDto>.NotFoundResult("Category not found");

            category.Name = request.Name;
            category.Description = request.Description;
            _unitOfWork.Categories.Update(category);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<CategoryResponseDto>.SuccessResult(MapToCategoryDto(category), "Category updated successfully");
        }

        public async Task<ServiceResult> DeleteCategoryAsync(long categoryId)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(categoryId);
            if (category == null) return ServiceResult.NotFoundResult("Category not found");

            var requestsCount = await _unitOfWork.Requests.CountAsync(r => r.CategoryId == categoryId);
            if (requestsCount > 0)
                return ServiceResult.FailResult("Cannot delete category that is in use");

            _unitOfWork.Categories.Remove(category);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Category deleted successfully");
        }

        // ── Category-Hashtag Mapping ──────────────────────────────────────

        public async Task<ServiceResult<List<CategoryHashtagMappingDto>>> GetCategoryHashtagMappingsAsync()
        {
            var mappings = await _unitOfWork.CategoryHashtags.GetAllWithDetailsAsync();

            var grouped = mappings
                .GroupBy(m => new { m.CategoryId, m.Category.Name })
                .Select(g => new CategoryHashtagMappingDto
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.Name,
                    Hashtags = g.Select(m => new CategoryHashtagItemDto
                    {
                        Id = m.HashtagId,
                        Name = m.Hashtag.Name
                    }).ToList()
                })
                .ToList();

            return ServiceResult<List<CategoryHashtagMappingDto>>.SuccessResult(grouped);
        }

        public async Task<ServiceResult<CategoryHashtagDetailDto>> GetHashtagsByCategoryAsync(long categoryId)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(categoryId);
            if (category == null)
                return ServiceResult<CategoryHashtagDetailDto>.NotFoundResult("Category not found");

            var hashtags = await _unitOfWork.CategoryHashtags.GetHashtagsByCategoryIdAsync(categoryId);

            var dto = new CategoryHashtagDetailDto
            {
                CategoryId = category.Id,
                CategoryName = category.Name,
                Hashtags = hashtags.Select(h => new CategoryHashtagItemDto
                {
                    Id = h.Id,
                    Name = h.Name
                }).ToList()
            };

            return ServiceResult<CategoryHashtagDetailDto>.SuccessResult(dto);
        }

        public async Task<ServiceResult> AddCategoryHashtagMappingAsync(AddCategoryHashtagDto request)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId);
            if (category == null) return ServiceResult.NotFoundResult("Category not found");

            var hashtag = await _unitOfWork.Hashtags.GetByIdAsync(request.HashtagId);
            if (hashtag == null) return ServiceResult.NotFoundResult("Hashtag not found");

            var exists = await _unitOfWork.CategoryHashtags.ExistsAsync(request.CategoryId, request.HashtagId);
            if (exists) return ServiceResult.FailResult("This mapping already exists");

            await _unitOfWork.CategoryHashtags.AddAsync(new CategoryHashtag
            {
                CategoryId = request.CategoryId,
                HashtagId = request.HashtagId
            });
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Category-Hashtag mapping created successfully");
        }

        public async Task<ServiceResult> RemoveCategoryHashtagMappingAsync(RemoveCategoryHashtagDto request)
        {
            var exists = await _unitOfWork.CategoryHashtags.ExistsAsync(request.CategoryId, request.HashtagId);
            if (!exists) return ServiceResult.NotFoundResult("Mapping not found");

            await _unitOfWork.CategoryHashtags.RemoveAsync(request.CategoryId, request.HashtagId);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Category-Hashtag mapping removed successfully");
        }

        // ── Hashtags ──────────────────────────────────────────────────────

        public async Task<ServiceResult<PagedResponse<AdminHashtagDto>>> GetAllHashtagsAsync(AdminSearchHashtagDto request)
        {
            var query = _unitOfWork.Hashtags.Query();

            if (!string.IsNullOrWhiteSpace(request.Keyword))
                query = query.Where(h => h.Name.Contains(request.Keyword));

            var totalCount = await query.CountAsync();

            var hashtags = await query
                .Include(h => h.MentorHashtags)
                .Include(h => h.CategoryHashtags)
                .OrderBy(h => h.Name)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var dtos = hashtags.Select(h => new AdminHashtagDto
            {
                Id = h.Id,
                Name = h.Name,
                MentorCount = h.MentorHashtags?.Count ?? 0,
                CategoryCount = h.CategoryHashtags?.Count ?? 0
            });

            var response = PagedResponse<AdminHashtagDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount);
            return ServiceResult<PagedResponse<AdminHashtagDto>>.SuccessResult(response);
        }

        public async Task<ServiceResult<List<HashtagDto>>> GetHashtagsAsync()
        {
            var hashtags = await _unitOfWork.Hashtags.Query()
                .OrderBy(h => h.Name)
                .ToListAsync();

            var dtos = hashtags.Select(h => new HashtagDto
            {
                Id = h.Id,
                Name = h.Name
            }).ToList();

            return ServiceResult<List<HashtagDto>>.SuccessResult(dtos);
        }

        public async Task<ServiceResult<AdminHashtagDto>> CreateHashtagAsync(CreateHashtagDto request)
        {
            var existing = await _unitOfWork.Hashtags.GetByNameAsync(request.Name);
            if (existing != null)
                return ServiceResult<AdminHashtagDto>.FailResult("Hashtag already exists");

            var hashtag = new Hashtag { Name = request.Name };
            await _unitOfWork.Hashtags.AddAsync(hashtag);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<AdminHashtagDto>.CreatedResult(new AdminHashtagDto
            {
                Id = hashtag.Id,
                Name = hashtag.Name,
                MentorCount = 0,
                CategoryCount = 0
            }, "Hashtag created successfully");
        }

        public async Task<ServiceResult> DeleteHashtagAsync(long hashtagId)
        {
            var hashtag = await _unitOfWork.Hashtags.Query()
                .Include(h => h.MentorHashtags)
                .FirstOrDefaultAsync(h => h.Id == hashtagId);

            if (hashtag == null) return ServiceResult.NotFoundResult("Hashtag not found");

            if (hashtag.MentorHashtags?.Any() == true)
                return ServiceResult.FailResult("Cannot delete hashtag that is in use");

            _unitOfWork.Hashtags.Remove(hashtag);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Hashtag deleted successfully");
        }

        // ── Private helpers ───────────────────────────────────────────────

        private static CategoryResponseDto MapToCategoryDto(Category category) => new()
        {
            Id = category.Id,
            Name = category.Name ?? string.Empty,
            Description = category.Description
        };
    }
}
