using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Community;
using MentorEdu.Services.Common;

namespace MentorEdu.Services.Interfaces
{
    public interface ICommunityService
    {
        // Posts
        Task<ServiceResult<PagedResponse<CommunityPostListDto>>> GetPostsAsync(SearchPostDto searchDto);
        Task<ServiceResult<CommunityPostDetailDto>> GetPostByIdAsync(long postId, long? currentUserId = null);
        Task<ServiceResult<CommunityPostDetailDto>> CreatePostAsync(long userId, CreatePostDto request);
        Task<ServiceResult<CommunityPostDetailDto>> UpdatePostAsync(long postId, long userId, UpdatePostDto request);
        Task<ServiceResult> DeletePostAsync(long postId, long userId);
        
        // Comments
        Task<ServiceResult<CommunityCommentDto>> CreateCommentAsync(long userId, CreateCommentDto request);
        Task<ServiceResult> DeleteCommentAsync(long commentId, long userId);
        Task<ServiceResult<List<CommunityCommentDto>>> GetCommentsByPostAsync(long postId);
        
        // Featured
        Task<ServiceResult<List<CommunityPostListDto>>> GetRecentPostsAsync(int count = 10);
        Task<ServiceResult<List<CommunityPostListDto>>> GetMyPostsAsync(long userId, int pageNumber, int pageSize);
    }
}
