using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Community;
using MentorEdu.Services.Common;

namespace MentorEdu.Services.Interfaces
{
    public interface ICommunityService
    {
        // Posts
        Task<ServiceResult<PagedResponse<CommunityPostListDto>>> GetPostsAsync(SearchPostDto searchDto, long? currentUserId = null);
        Task<ServiceResult<CommunityPostDetailDto>> GetPostByIdAsync(long postId, long? currentUserId = null);
        Task<ServiceResult<CommunityPostDetailDto>> CreatePostAsync(long userId, CreatePostDto request);
        Task<ServiceResult<CommunityPostDetailDto>> UpdatePostAsync(long postId, long userId, UpdatePostDto request);
        Task<ServiceResult> DeletePostAsync(long postId, long userId);
        
        // Comments
        Task<ServiceResult<CommunityCommentDto>> CreateCommentAsync(long userId, CreateCommentDto request);
        Task<ServiceResult> DeleteCommentAsync(long commentId, long userId);
        Task<ServiceResult<List<CommunityCommentDto>>> GetCommentsByPostAsync(long postId);
        
        // Featured
        Task<ServiceResult<List<CommunityPostListDto>>> GetRecentPostsAsync(int count = 10, long? currentUserId = null);
        Task<ServiceResult<List<CommunityPostListDto>>> GetMyPostsAsync(long userId, int pageNumber, int pageSize);
        Task<ServiceResult<PagedResponse<CommunityPostListDto>>> GetFollowingPostsAsync(long userId, int pageNumber, int pageSize);
        
        // Trending
        Task<ServiceResult<List<TrendingHashtagDto>>> GetTrendingHashtagsAsync(int count = 10);
        
        // Proposals on Posts
        Task<ServiceResult<CommunityProposalDto>> SendProposalAsync(long mentorId, long postId, SendProposalDto request);
        Task<ServiceResult<List<CommunityProposalDto>>> GetProposalsByPostAsync(long postId);
        Task<ServiceResult<AcceptProposalResultDto>> AcceptProposalAsync(long userId, long proposalId);
        Task<ServiceResult> RejectProposalAsync(long userId, long proposalId);
        Task<ServiceResult<List<CommunityProposalDto>>> GetMyProposalsAsync(long userId);
        Task<ServiceResult<List<CommunityProposalDto>>> GetReceivedProposalsAsync(long userId);
        
        // Follow
        Task<ServiceResult> FollowUserAsync(long followerId, long followingId);
        Task<ServiceResult> UnfollowUserAsync(long followerId, long followingId);
        Task<ServiceResult<bool>> IsFollowingAsync(long followerId, long followingId);
    }
}
