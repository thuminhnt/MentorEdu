using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Community;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using MentorEdu.Services.Common;
using MentorEdu.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Services.Implementations
{
    public class CommunityService : BaseService, ICommunityService
    {
        private readonly INotificationService _notificationService;

        public CommunityService(IUnitOfWork unitOfWork, INotificationService notificationService) : base(unitOfWork)
        {
            _notificationService = notificationService;
        }

        #region Posts


        public async Task<ServiceResult<PagedResponse<CommunityPostListDto>>> GetPostsAsync(SearchPostDto searchDto, long? currentUserId = null)
        {
            // Build query with filters
            var query = _unitOfWork.CommunityPosts.Query()
                .Include(p => p.Author).ThenInclude(a => a.UserProfile)
                .Include(p => p.Category)
                .Include(p => p.PostHashtags).ThenInclude(ph => ph.Hashtag)
                .Include(p => p.Files)
                .Include(p => p.Comments)
                .Include(p => p.Proposals)
                .AsQueryable();

            // Keyword filter
            if (!string.IsNullOrWhiteSpace(searchDto.Keyword))
            {
                query = query.Where(p => p.Title!.Contains(searchDto.Keyword) || 
                                          p.Content!.Contains(searchDto.Keyword));
            }

            // Hashtag filter
            if (!string.IsNullOrWhiteSpace(searchDto.Hashtag))
            {
                query = query.Where(p => p.PostHashtags.Any(ph => ph.Hashtag!.Name == searchDto.Hashtag));
            }

            // Category filter (simple, no hierarchy)
            if (searchDto.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId.HasValue && p.CategoryId.Value == searchDto.CategoryId.Value);
            }

            var totalCount = await query.CountAsync();

            var posts = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((searchDto.PageNumber - 1) * searchDto.PageSize)
                .Take(searchDto.PageSize)
                .ToListAsync();

            var dtos = posts.Select(p => MapToPostListDto(p, currentUserId));
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
                CategoryId = request.CategoryId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.CommunityPosts.AddAsync(post);
            await _unitOfWork.SaveChangesAsync();

            // Handle hashtags - create or lookup existing
            if (request.Hashtags != null && request.Hashtags.Count > 0)
            {
                foreach (var tagName in request.Hashtags.Take(10)) // Limit to 10 hashtags
                {
                    var normalizedName = tagName.Trim().ToLower().TrimStart('#');
                    if (string.IsNullOrEmpty(normalizedName)) continue;

                    // Try to find existing hashtag
                    var hashtag = await _unitOfWork.Hashtags.GetByNameAsync(normalizedName);
                    
                    if (hashtag == null)
                    {
                        // Create new hashtag
                        hashtag = new Hashtag { Name = normalizedName };
                        await _unitOfWork.Hashtags.AddAsync(hashtag);
                        await _unitOfWork.SaveChangesAsync();
                    }

                    // Create post-hashtag link
                    var postHashtag = new CommunityPostHashtag
                    {
                        PostId = post.Id,
                        HashtagId = hashtag.Id
                    };
                    await _unitOfWork.CommunityPostHashtags.AddAsync(postHashtag);
                }
                await _unitOfWork.SaveChangesAsync();
            }

            // Handle file attachments
            if (request.FileUrls != null && request.FileUrls.Count > 0)
            {
                foreach (var fileUrl in request.FileUrls.Take(2)) // Limit to 2 files
                {
                    if (string.IsNullOrEmpty(fileUrl)) continue;

                    var urlFileName = Path.GetFileName(fileUrl);
                    var extension = Path.GetExtension(fileUrl).ToLower();
                    var fileType = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(extension) 
                        ? "image" 
                        : "document";

                    // Extract original filename from the URL (format: {guid}_{originalName}.ext)
                    var displayFileName = urlFileName;
                    var underscoreIndex = urlFileName.IndexOf('_');
                    if (underscoreIndex > 0 && underscoreIndex < urlFileName.Length - 1)
                    {
                        displayFileName = urlFileName.Substring(underscoreIndex + 1);
                    }

                    var postFile = new CommunityPostFile
                    {
                        PostId = post.Id,
                        FileUrl = fileUrl,
                        FileName = displayFileName,
                        FileType = fileType,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.CommunityPostFiles.AddAsync(postFile);
                }
                await _unitOfWork.SaveChangesAsync();
            }

            // Notify mentors about relevant post based on category and hashtags
            await _notificationService.NotifyMentorsAboutRelevantPostAsync(
                post.Id, 
                request.CategoryId, 
                request.Hashtags ?? new List<string>()
            );

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

            // Notify post author about new comment (if not commenting on own post)
            if (post.AuthorId != userId)
            {
                await _notificationService.CreateNotificationAsync(
                    post.AuthorId,
                    NotificationType.NewComment,
                    "New Comment on Your Post",
                    $"{user.UserProfile?.FullName ?? "Someone"} commented on your post",
                    $"{{\"postId\": {post.Id}, \"commentId\": {comment.Id}}}"
                );
            }

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

        public async Task<ServiceResult<List<CommunityPostListDto>>> GetRecentPostsAsync(int count = 10, long? currentUserId = null)
        {
            var posts = await _unitOfWork.CommunityPosts.GetRecentPostsAsync(count);
            var dtos = posts.Select(p => MapToPostListDto(p, currentUserId)).ToList();

            return ServiceResult<List<CommunityPostListDto>>.SuccessResult(dtos);
        }

        public async Task<ServiceResult<List<CommunityPostListDto>>> GetMyPostsAsync(long userId, int pageNumber, int pageSize)
        {
            var posts = await _unitOfWork.CommunityPosts.GetByAuthorIdAsync(userId);
            
            var pagedPosts = posts
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);

            var dtos = pagedPosts.Select(p => MapToPostListDto(p, userId)).ToList();

            return ServiceResult<List<CommunityPostListDto>>.SuccessResult(dtos);
        }

        #endregion

        #region Following Posts

        public async Task<ServiceResult<PagedResponse<CommunityPostListDto>>> GetFollowingPostsAsync(long userId, int pageNumber, int pageSize)
        {
            var followingIds = await _unitOfWork.UserFollows.GetFollowingIdsAsync(userId);
            
            if (!followingIds.Any())
            {
                var emptyResponse = PagedResponse<CommunityPostListDto>.Create(
                    Enumerable.Empty<CommunityPostListDto>(), pageNumber, pageSize, 0);
                return ServiceResult<PagedResponse<CommunityPostListDto>>.SuccessResult(emptyResponse);
            }

            var (posts, totalCount) = await _unitOfWork.CommunityPosts.GetPagedAsync(
                pageNumber, pageSize, null, followingIds.ToList());

            var dtos = posts.Select(p => MapToPostListDto(p, userId));
            var response = PagedResponse<CommunityPostListDto>.Create(dtos, pageNumber, pageSize, totalCount);

            return ServiceResult<PagedResponse<CommunityPostListDto>>.SuccessResult(response);
        }

        public async Task<ServiceResult<List<TrendingHashtagDto>>> GetTrendingHashtagsAsync(int count = 10)
        {
            // Get hashtags with their post counts
            var hashtagPostCounts = await _unitOfWork.CommunityPostHashtags.Query()
                .GroupBy(cph => new { cph.HashtagId, cph.Hashtag!.Name })
                .Select(g => new TrendingHashtagDto
                {
                    Id = g.Key.HashtagId,
                    Name = g.Key.Name ?? "",
                    PostCount = g.Count()
                })
                .OrderByDescending(h => h.PostCount)
                .Take(count)
                .ToListAsync();

            return ServiceResult<List<TrendingHashtagDto>>.SuccessResult(hashtagPostCounts);
        }

        #endregion

        #region Proposals

        public async Task<ServiceResult<CommunityProposalDto>> SendProposalAsync(long mentorId, long postId, SendProposalDto request)
        {
            var mentor = await _unitOfWork.Users.GetByIdWithProfilesAsync(mentorId);
            if (mentor == null || mentor.Role != UserRole.Mentor)
                return ServiceResult<CommunityProposalDto>.ForbiddenResult("Only mentors can send proposals");

            var post = await _unitOfWork.CommunityPosts.GetByIdAsync(postId);
            if (post == null)
                return ServiceResult<CommunityProposalDto>.NotFoundResult("Post not found");

            if (post.AuthorId == mentorId)
                return ServiceResult<CommunityProposalDto>.BadRequestResult("You cannot send a proposal to your own post");

            // Check if already sent proposal
            var existingProposal = await _unitOfWork.Proposals.GetByPostAndMentorAsync(postId, mentorId);
            if (existingProposal != null)
                return ServiceResult<CommunityProposalDto>.BadRequestResult("You have already sent a proposal for this post");

            var proposal = new Proposal
            {
                PostId = postId,
                MentorId = mentorId,
                Price = request.Price,
                EstimatedHours = request.EstimatedHours,
                Message = request.Message,
                Status = ProposalStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Proposals.AddAsync(proposal);
            await _unitOfWork.SaveChangesAsync();

            // Notify post author
            await _notificationService.CreateNotificationAsync(
                post.AuthorId,
                NotificationType.NewProposal,
                "New Proposal Received",
                $"{mentor.UserProfile?.FullName ?? "A mentor"} sent you a proposal for your post",
                $"{{\"proposalId\": {proposal.Id}, \"postId\": {postId}}}"
            );

            var dto = MapToProposalDto(proposal, post, mentor);
            return ServiceResult<CommunityProposalDto>.CreatedResult(dto, "Proposal sent successfully");
        }

        public async Task<ServiceResult<List<CommunityProposalDto>>> GetProposalsByPostAsync(long postId)
        {
            var proposals = await _unitOfWork.Proposals.GetByPostIdAsync(postId);
            var post = await _unitOfWork.CommunityPosts.GetByIdAsync(postId);

            var dtos = proposals.Select(p => MapToProposalDto(p, post, p.Mentor)).ToList();
            return ServiceResult<List<CommunityProposalDto>>.SuccessResult(dtos);
        }

        public async Task<ServiceResult<AcceptProposalResultDto>> AcceptProposalAsync(long userId, long proposalId)
        {
            var proposal = await _unitOfWork.Proposals.GetByIdWithDetailsAsync(proposalId);
            if (proposal == null)
                return ServiceResult<AcceptProposalResultDto>.NotFoundResult("Proposal not found");

            if (proposal.Post == null)
                return ServiceResult<AcceptProposalResultDto>.BadRequestResult("Proposal is not linked to a post");

            if (proposal.Post.AuthorId != userId)
                return ServiceResult<AcceptProposalResultDto>.ForbiddenResult("You can only accept proposals for your own posts");

            if (proposal.Status != ProposalStatus.Pending)
                return ServiceResult<AcceptProposalResultDto>.BadRequestResult($"Proposal is already {proposal.Status}");

            proposal.Status = ProposalStatus.Accepted;
            proposal.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Proposals.Update(proposal);

            // Create conversation between student and mentor
            var conversation = new Conversation
            {
                StudentId = userId,
                MentorId = proposal.MentorId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Conversations.AddAsync(conversation);
            await _unitOfWork.SaveChangesAsync();

            // Notify mentor
            await _notificationService.CreateNotificationAsync(
                proposal.MentorId,
                NotificationType.ProposalAccepted,
                "Proposal Accepted",
                $"Your proposal has been accepted! Start a conversation now.",
                $"{{\"proposalId\": {proposalId}, \"conversationId\": {conversation.Id}}}"
            );

            return ServiceResult<AcceptProposalResultDto>.SuccessResult(new AcceptProposalResultDto
            {
                ProposalId = proposalId,
                ConversationId = conversation.Id,
                Message = "Proposal accepted successfully. Conversation created."
            });
        }

        public async Task<ServiceResult> RejectProposalAsync(long userId, long proposalId)
        {
            var proposal = await _unitOfWork.Proposals.GetByIdWithDetailsAsync(proposalId);
            if (proposal == null)
                return ServiceResult.NotFoundResult("Proposal not found");

            if (proposal.Post == null)
                return ServiceResult.BadRequestResult("Proposal is not linked to a post");

            if (proposal.Post.AuthorId != userId)
                return ServiceResult.ForbiddenResult("You can only reject proposals for your own posts");

            if (proposal.Status != ProposalStatus.Pending)
                return ServiceResult.BadRequestResult($"Proposal is already {proposal.Status}");

            proposal.Status = ProposalStatus.Rejected;
            proposal.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Proposals.Update(proposal);
            await _unitOfWork.SaveChangesAsync();

            // Notify mentor
            await _notificationService.CreateNotificationAsync(
                proposal.MentorId,
                NotificationType.ProposalRejected,
                "Proposal Rejected",
                "Your proposal has been rejected.",
                $"{{\"proposalId\": {proposalId}}}"
            );

            return ServiceResult.SuccessResult("Proposal rejected");
        }

        public async Task<ServiceResult<List<CommunityProposalDto>>> GetMyProposalsAsync(long userId)
        {
            var proposals = await _unitOfWork.Proposals.GetByMentorIdAsync(userId);
            var postProposals = proposals.Where(p => p.PostId != null);

            var dtos = postProposals.Select(p => MapToProposalDto(p, p.Post, p.Mentor)).ToList();
            return ServiceResult<List<CommunityProposalDto>>.SuccessResult(dtos);
        }

        public async Task<ServiceResult<List<CommunityProposalDto>>> GetReceivedProposalsAsync(long userId)
        {
            var proposals = await _unitOfWork.Proposals.GetByPostAuthorIdAsync(userId);

            var dtos = proposals.Select(p => MapToProposalDto(p, p.Post, p.Mentor)).ToList();
            return ServiceResult<List<CommunityProposalDto>>.SuccessResult(dtos);
        }

        #endregion

        #region Follow

        public async Task<ServiceResult> FollowUserAsync(long followerId, long followingId)
        {
            if (followerId == followingId)
                return ServiceResult.BadRequestResult("You cannot follow yourself");

            var followingUser = await _unitOfWork.Users.GetByIdWithProfilesAsync(followingId);
            if (followingUser == null)
                return ServiceResult.NotFoundResult("User not found");

            var followerUser = await _unitOfWork.Users.GetByIdWithProfilesAsync(followerId);

            var existingFollow = await _unitOfWork.UserFollows.GetByFollowerAndFollowingAsync(followerId, followingId);
            if (existingFollow != null)
                return ServiceResult.BadRequestResult("You are already following this user");

            var follow = new UserFollow
            {
                FollowerId = followerId,
                FollowingId = followingId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.UserFollows.AddAsync(follow);
            await _unitOfWork.SaveChangesAsync();

            // Notify the user being followed
            await _notificationService.CreateNotificationAsync(
                followingId,
                NotificationType.NewFollower,
                "New Follower",
                $"{followerUser?.UserProfile?.FullName ?? "Someone"} started following you",
                $"{{\"followerId\": {followerId}}}"
            );

            return ServiceResult.SuccessResult("Successfully followed user");
        }

        public async Task<ServiceResult> UnfollowUserAsync(long followerId, long followingId)
        {
            var follow = await _unitOfWork.UserFollows.GetByFollowerAndFollowingAsync(followerId, followingId);
            if (follow == null)
                return ServiceResult.NotFoundResult("You are not following this user");

            _unitOfWork.UserFollows.Remove(follow);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Successfully unfollowed user");
        }

        public async Task<ServiceResult<bool>> IsFollowingAsync(long followerId, long followingId)
        {
            var isFollowing = await _unitOfWork.UserFollows.IsFollowingAsync(followerId, followingId);
            return ServiceResult<bool>.SuccessResult(isFollowing);
        }

        #endregion

        #region Private Methods

        private CommunityPostListDto MapToPostListDto(CommunityPost post, long? currentUserId = null)
        {
            var isFollowing = false;
            if (currentUserId.HasValue && currentUserId.Value != post.AuthorId)
            {
                isFollowing = _unitOfWork.UserFollows.IsFollowingAsync(currentUserId.Value, post.AuthorId).GetAwaiter().GetResult();
            }

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
                AuthorRole = post.Author?.Role.ToString(),
                IsAuthorMentor = post.Author?.Role == UserRole.Mentor,
                IsFollowing = isFollowing,
                CommentCount = post.Comments?.Count ?? 0,
                ProposalCount = post.Proposals?.Count ?? 0,
                // Category
                CategoryId = post.CategoryId,
                CategoryName = post.Category?.Name,
                // Hashtags
                Hashtags = post.PostHashtags?.Select(ph => ph.Hashtag?.Name ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>(),
                // Files
                Files = post.Files?.Select(f => new PostFileDto
                {
                    Id = f.Id,
                    FileUrl = f.FileUrl,
                    FileName = f.FileName,
                    FileType = f.FileType,
                    FileSize = f.FileSize
                }).ToList() ?? new List<PostFileDto>()
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
                ProposalCount = post.Proposals?.Count ?? 0,
                // Category
                CategoryId = post.CategoryId,
                CategoryName = post.Category?.Name,
                // Hashtags
                Hashtags = post.PostHashtags?.Select(ph => ph.Hashtag?.Name ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>(),
                // Files
                Files = post.Files?.Select(f => new PostFileDto
                {
                    Id = f.Id,
                    FileUrl = f.FileUrl,
                    FileName = f.FileName,
                    FileType = f.FileType,
                    FileSize = f.FileSize
                }).ToList() ?? new List<PostFileDto>(),
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

        private CommunityProposalDto MapToProposalDto(Proposal proposal, CommunityPost? post, User? mentor)
        {
            return new CommunityProposalDto
            {
                Id = proposal.Id,
                PostId = proposal.PostId ?? 0,
                PostTitle = post?.Title,
                MentorId = proposal.MentorId,
                MentorName = mentor?.UserProfile?.FullName,
                MentorAvatar = mentor?.UserProfile?.AvatarUrl,
                MentorTitle = mentor?.MentorProfile?.Title,
                Price = proposal.Price,
                EstimatedHours = proposal.EstimatedHours,
                Message = proposal.Message,
                Status = proposal.Status.ToString(),
                CreatedAt = proposal.CreatedAt
            };
        }

        public async Task<ServiceResult<FollowListDto>> GetFollowersAsync(long userId)
        {
            var followers = await _unitOfWork.UserFollows.GetFollowersByUserIdAsync(userId);
            var count = await _unitOfWork.UserFollows.GetFollowerCountAsync(userId);

            var users = new List<FollowUserDto>();
            foreach (var follow in followers)
            {
                var follower = await _unitOfWork.Users.GetByIdAsync(follow.FollowerId);
                if (follower != null)
                {
                    var profile = await _unitOfWork.UserProfiles.GetByUserIdAsync(follower.Id);
                    users.Add(new FollowUserDto
                    {
                        Id = follower.Id,
                        FullName = profile?.FullName ?? "Unknown",
                        AvatarUrl = profile?.AvatarUrl,
                        Role = follower.Role.ToString(),
                        IsMentor = follower.Role == UserRole.Mentor
                    });
                }
            }

            return ServiceResult<FollowListDto>.SuccessResult(new FollowListDto
            {
                Count = count,
                Users = users
            });
        }

        public async Task<ServiceResult<FollowListDto>> GetFollowingAsync(long userId)
        {
            var following = await _unitOfWork.UserFollows.GetFollowingByUserIdAsync(userId);
            var count = await _unitOfWork.UserFollows.GetFollowingCountAsync(userId);

            var users = new List<FollowUserDto>();
            foreach (var follow in following)
            {
                var followedUser = await _unitOfWork.Users.GetByIdAsync(follow.FollowingId);
                if (followedUser != null)
                {
                    var profile = await _unitOfWork.UserProfiles.GetByUserIdAsync(followedUser.Id);
                    users.Add(new FollowUserDto
                    {
                        Id = followedUser.Id,
                        FullName = profile?.FullName ?? "Unknown",
                        AvatarUrl = profile?.AvatarUrl,
                        Role = followedUser.Role.ToString(),
                        IsMentor = followedUser.Role == UserRole.Mentor
                    });
                }
            }

            return ServiceResult<FollowListDto>.SuccessResult(new FollowListDto
            {
                Count = count,
                Users = users
            });
        }

        #endregion
    }
}
