using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MentorEdu.Models.Enums;

namespace MentorEdu.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("role")]
        public UserRole Role { get; set; }

        [Column("is_verified")]
        public bool IsVerified { get; set; } = false;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual UserProfile? UserProfile { get; set; }
        public virtual MentorProfile? MentorProfile { get; set; }
        public virtual Wallet? Wallet { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
        public virtual ICollection<Proposal> Proposals { get; set; } = new List<Proposal>();
        public virtual ICollection<Order> StudentOrders { get; set; } = new List<Order>();
        public virtual ICollection<Order> MentorOrders { get; set; } = new List<Order>();
        public virtual ICollection<Review> ReviewsGiven { get; set; } = new List<Review>();
        public virtual ICollection<Review> ReviewsReceived { get; set; } = new List<Review>();
        public virtual ICollection<Report> ReportsCreated { get; set; } = new List<Report>();
        public virtual ICollection<Report> ReportsReceived { get; set; } = new List<Report>();
        public virtual ICollection<CommunityPost> CommunityPosts { get; set; } = new List<CommunityPost>();
        public virtual ICollection<CommunityComment> CommunityComments { get; set; } = new List<CommunityComment>();
        public virtual ICollection<MentorCategory> MentorCategories { get; set; } = new List<MentorCategory>();
        public virtual ICollection<MentorHashtag> MentorHashtags { get; set; } = new List<MentorHashtag>();
    }
}
