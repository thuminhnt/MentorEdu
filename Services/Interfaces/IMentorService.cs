using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Review;
using MentorEdu.DTOs.User;
using MentorEdu.Services.Common;

namespace MentorEdu.Services.Interfaces
{
    public interface IMentorService
    {
        // Tìm kiếm & Lọc
        Task<ServiceResult<PagedResponse<MentorListDto>>> SearchMentorsAsync(SearchMentorRequest request);
        Task<ServiceResult<List<MentorListDto>>> GetTopMentorsAsync(int count = 10);
        Task<ServiceResult<List<MentorListDto>>> GetMentorsByCategoryAsync(long categoryId);
        
        // Chi tiết
        Task<ServiceResult<UserDetailDto>> GetMentorDetailAsync(long mentorId);
        
        // Đánh giá
        Task<ServiceResult<PagedResponse<MentorReviewDto>>> GetMentorReviewsAsync(long mentorId, int pageNumber, int pageSize);
        Task<ServiceResult<ReviewSummaryDto>> GetMentorReviewSummaryAsync(long mentorId);

        // Quy trình đăng ký mentor
        Task<ServiceResult> ApplyAsMentorAsync(long userId, ApplyAsMentorRequest request);
        Task<ServiceResult<MentorApplicationStatusDto>> GetApplicationStatusAsync(long userId);
        Task<ServiceResult> ConfirmSwitchToMentorAsync(long userId);
    }
}
