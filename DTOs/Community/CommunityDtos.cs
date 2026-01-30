using System.ComponentModel.DataAnnotations;

namespace MentorEdu.DTOs.Community
{
    // === SEARCH / FILTER ===
    public class SearchPostDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Keyword { get; set; }
        public long? AuthorId { get; set; }
        public List<string>? Tags { get; set; }
        public string? Hashtag { get; set; }  // Filter by single hashtag
        public long? CategoryId { get; set; }  // Filter by category
        public string? SortBy { get; set; } = "createdAt";
        public bool SortDescending { get; set; } = true;
    }

    // === TRENDING ===
    public class TrendingHashtagDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int PostCount { get; set; }
    }

    // === POST DTOs ===
    public class CommunityPostListDto
    {
        public long Id { get; set; }
        public string? Title { get; set; }
        public string? ContentPreview { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public long AuthorId { get; set; }
        public string? AuthorName { get; set; }
        public string? AuthorAvatar { get; set; }
        public string? AuthorRole { get; set; }
        public bool IsAuthorMentor { get; set; }
        public bool IsFollowing { get; set; }
        public int CommentCount { get; set; }
        public int ProposalCount { get; set; }
        
        // Category & Hashtags
        public long? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public List<string> Hashtags { get; set; } = new();
        
        // Files
        public List<PostFileDto> Files { get; set; } = new();
    }

    public class CommunityPostDetailDto
    {
        public long Id { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public PostAuthorDto Author { get; set; } = null!;
        public int CommentCount { get; set; }
        public int ProposalCount { get; set; }
        public List<CommunityCommentDto>? Comments { get; set; }
        public List<CommunityProposalDto>? Proposals { get; set; }
        public bool IsFollowing { get; set; }
        
        // Category & Hashtags
        public long? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public List<string> Hashtags { get; set; } = new();
        
        // Files
        public List<PostFileDto> Files { get; set; } = new();
    }

    public class PostAuthorDto
    {
        public long Id { get; set; }
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Title { get; set; }
        public bool IsMentor { get; set; }
    }

    public class CreatePostDto
    {
        public string? Title { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public List<string>? Hashtags { get; set; }  // Hashtag names
        public long? CategoryId { get; set; }
        public List<string>? FileUrls { get; set; }  // Pre-uploaded file URLs
    }

    public class UpdatePostDto
    {
        public string? Title { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public List<string>? Hashtags { get; set; }
        public long? CategoryId { get; set; }
        public List<string>? FileUrls { get; set; }
    }

    public class PostFileDto
    {
        public long Id { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string FileType { get; set; } = "document";
        public long? FileSize { get; set; }
    }

    // === COMMENT DTOs ===
    public class CommunityCommentDto
    {
        public long Id { get; set; }
        public string? Content { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long AuthorId { get; set; }
        public string? AuthorName { get; set; }
        public string? AuthorAvatar { get; set; }
        public bool IsAuthorMentor { get; set; }
    }

    public class CreateCommentDto
    {
        public long PostId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;
    }

    // === PROPOSAL DTOs ===
    public class CommunityProposalDto
    {
        public long Id { get; set; }
        public long PostId { get; set; }
        public string? PostTitle { get; set; }
        public long MentorId { get; set; }
        public string? MentorName { get; set; }
        public string? MentorAvatar { get; set; }
        public string? MentorTitle { get; set; }
        public decimal? Price { get; set; }
        public decimal? EstimatedHours { get; set; }
        public string? Message { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime? CreatedAt { get; set; }
    }

    public class SendProposalDto
    {
        public decimal? Price { get; set; }
        public decimal? EstimatedHours { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;
    }

    public class AcceptProposalResultDto
    {
        public long ProposalId { get; set; }
        public long ConversationId { get; set; }
        public string Message { get; set; } = "Proposal accepted successfully";
    }
}
