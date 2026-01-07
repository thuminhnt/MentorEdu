using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Community;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using MentorEdu.Services.Common;
using MentorEdu.Services.Interfaces;

namespace MentorEdu.Services.Implementations
{
    public class CommunityService : BaseService, ICommunityService
    {
        public CommunityService(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
        }

        #region Posts

        public async Task<ServiceResult<PagedResponse<CommunityPostListDto>>> GetPostsAsync(SearchPostDto searchDto)
        {
            var (posts, totalCount) = await _unitOfWork.CommunityPosts.GetPagedAsync(
                searchDto.PageNumber, searchDto.PageSize, searchDto.Keyword);

            var dtos = posts.Select(MapToPostListDto);
            var response = PagedResponse<CommunityPostListDto>.Create(dtos, searchDto.PageNumber, searchDto.PageSize, totalCount);

            return ServiceResult<PagedResponse<CommunityPostListDto>>.SuccessResult(response);
        }

        public async Task<ServiceResult<CommunityPostDetailDto>> GetPostByIdAsync(long postId, long? currentUserId = null)
        {
            var post = await _unitOfWork.CommunityPosts.GetByIdWithCommentsAsync(postId);
            
            if (post == null)
                return ServiceResult<CommunityPostDetailDto>.NotFoundResult("Post not found");

            var dto = MapToPostDetailDto(post);
            return ServiceResult<CommunityPostDetailDto>.SuccessResult(dto);
        }

        public async Task<ServiceResult<CommunityPostDetailDto>> CreatePostAsync(long userId, CreatePostDto request)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return ServiceResult<CommunityPostDetailDto>.NotFoundResult("User not found");

            var post = new CommunityPost
            {
                AuthorId = userId,
                Title = request.Title,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.CommunityPosts.AddAsync(post);
            await _unitOfWork.SaveChangesAsync();

            return await GetPostByIdAsync(post.Id);
        }

        public async Task<ServiceResult<CommunityPostDetailDto>> UpdatePostAsync(long postId, long userId, UpdatePostDto request)
        {
            var post = await _unitOfWork.CommunityPosts.GetByIdAsync(postId);
            
            if (post == null)
                return ServiceResult<CommunityPostDetailDto>.NotFoundResult("Post not found");

            if (post.AuthorId != userId)
                return ServiceResult<CommunityPostDetailDto>.ForbiddenResult("You do not have permission to edit this post");

            post.Title = request.Title;
            post.Content = request.Content;
            post.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.CommunityPosts.Update(post);
            await _unitOfWork.SaveChangesAsync();

            return await GetPostByIdAsync(postId);
        }

        public async Task<ServiceResult> DeletePostAsync(long postId, long userId)
        {
            var post = await _unitOfWork.CommunityPosts.GetByIdAsync(postId);
            
            if (post == null)
                return ServiceResult.NotFoundResult("Post not found");

            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            
            // Allow delete if owner or admin
            if (post.AuthorId != userId && user?.Role != UserRole.Admin)
                return ServiceResult.ForbiddenResult("You do not have permission to delete this post");

            _unitOfWork.CommunityPosts.Remove(post);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Post deleted successfully");
        }

        #endregion

        #region Comments

        public async Task<ServiceResult<CommunityCommentDto>> CreateCommentAsync(long userId, CreateCommentDto request)
        {
            var post = await _unitOfWork.CommunityPosts.GetByIdAsync(request.PostId);
            
            if (post == null)
                return ServiceResult<CommunityCommentDto>.NotFoundResult("Post not found");

            var user = await _unitOfWork.Users.GetByIdWithProfilesAsync(userId);
            if (user == null)
                return ServiceResult<CommunityCommentDto>.NotFoundResult("User not found");

            var comment = new CommunityComment
            {
                PostId = request.PostId,
                AuthorId = userId,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.CommunityComments.AddAsync(comment);
            await _unitOfWork.SaveChangesAsync();

            var dto = new CommunityCommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                AuthorId = userId,
                AuthorName = user.UserProfile?.FullName,
                AuthorAvatar = user.UserProfile?.AvatarUrl,
                IsAuthorMentor = user.Role == UserRole.Mentor
            };

            return ServiceResult<CommunityCommentDto>.CreatedResult(dto, "Comment posted successfully");
        }

        public async Task<ServiceResult> DeleteCommentAsync(long commentId, long userId)
        {
            var comment = await _unitOfWork.CommunityComments.GetByIdAsync(commentId);
            
            if (comment == null)
                return ServiceResult.NotFoundResult("Comment not found");

            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            
            // Allow delete if owner or admin
            if (comment.AuthorId != userId && user?.Role != UserRole.Admin)
                return ServiceResult.ForbiddenResult("You do not have permission to delete this comment");

            _unitOfWork.CommunityComments.Remove(comment);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Comment deleted successfully");
        }

        public async Task<ServiceResult<List<CommunityCommentDto>>> GetCommentsByPostAsync(long postId)
        {
            var comments = await _unitOfWork.CommunityComments.GetByPostIdAsync(postId);

            var dtos = comments.Select(c => new CommunityCommentDto
            {
                Id = c.Id,
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                AuthorId = c.AuthorId,
                AuthorName = c.Author?.UserProfile?.FullName,
                AuthorAvatar = c.Author?.UserProfile?.AvatarUrl,
                IsAuthorMentor = c.Author?.Role == UserRole.Mentor
            }).ToList();

            return ServiceResult<List<CommunityCommentDto>>.SuccessResult(dtos);
        }

        #endregion

        #region Featured

        public async Task<ServiceResult<List<CommunityPostListDto>>> GetRecentPostsAsync(int count = 10)
        {
            var posts = await _unitOfWork.CommunityPosts.GetRecentPostsAsync(count);
            var dtos = posts.Select(MapToPostListDto).ToList();

            return ServiceResult<List<CommunityPostListDto>>.SuccessResult(dtos);
        }

        public async Task<ServiceResult<List<CommunityPostListDto>>> GetMyPostsAsync(long userId, int pageNumber, int pageSize)
        {
            var posts = await _unitOfWork.CommunityPosts.GetByAuthorIdAsync(userId);
            
            var pagedPosts = posts
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);

            var dtos = pagedPosts.Select(MapToPostListDto).ToList();

            return ServiceResult<List<CommunityPostListDto>>.SuccessResult(dtos);
        }

        #endregion

        #region Private Methods

        private CommunityPostListDto MapToPostListDto(CommunityPost post)
        {
            return new CommunityPostListDto
            {
                Id = post.Id,
                Title = post.Title,
                ContentPreview = post.Content?.Length > 200 
                    ? post.Content.Substring(0, 200) + "..." 
                    : post.Content,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                AuthorId = post.AuthorId,
                AuthorName = post.Author?.UserProfile?.FullName,
                AuthorAvatar = post.Author?.UserProfile?.AvatarUrl,
                CommentCount = post.Comments?.Count ?? 0
            };
        }

        private CommunityPostDetailDto MapToPostDetailDto(CommunityPost post)
        {
            return new CommunityPostDetailDto
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                Author = new PostAuthorDto
                {
                    Id = post.AuthorId,
                    FullName = post.Author?.UserProfile?.FullName,
                    AvatarUrl = post.Author?.UserProfile?.AvatarUrl,
                    Title = post.Author?.MentorProfile?.Title,
                    IsMentor = post.Author?.Role == UserRole.Mentor
                },
                CommentCount = post.Comments?.Count ?? 0,
                Comments = post.Comments?.Select(c => new CommunityCommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    AuthorId = c.AuthorId,
                    AuthorName = c.Author?.UserProfile?.FullName,
                    AuthorAvatar = c.Author?.UserProfile?.AvatarUrl,
                    IsAuthorMentor = c.Author?.Role == UserRole.Mentor
                }).ToList()
            };
        }

        #endregion
    }
}
