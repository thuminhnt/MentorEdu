using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Review;
using MentorEdu.DTOs.User;
using MentorEdu.Services.Common;

namespace MentorEdu.Services.Interfaces
{
    public interface IMentorService
    {
        // Search & Filter
        Task<ServiceResult<PagedResponse<MentorListDto>>> SearchMentorsAsync(SearchMentorRequest request);
        Task<ServiceResult<List<MentorListDto>>> GetTopMentorsAsync(int count = 10);
        Task<ServiceResult<List<MentorListDto>>> GetMentorsByCategoryAsync(long categoryId);
        
        // Detail
        Task<ServiceResult<UserDetailDto>> GetMentorDetailAsync(long mentorId);
        
        // Reviews
        Task<ServiceResult<PagedResponse<MentorReviewDto>>> GetMentorReviewsAsync(long mentorId, int pageNumber, int pageSize);
        Task<ServiceResult<ReviewSummaryDto>> GetMentorReviewSummaryAsync(long mentorId);
    }
}
