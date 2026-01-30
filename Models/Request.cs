using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MentorEdu.Models.Enums;

namespace MentorEdu.Models
{
    [Table("requests")]
    public class Request
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("student_id")]
        public long StudentId { get; set; }


        [MaxLength(255)]
        [Column("title")]
        public string? Title { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("category_id")]
        public long? CategoryId { get; set; }

        [Column("expected_budget")]
        public decimal? ExpectedBudget { get; set; }

        [Column("expected_hours")]
        public decimal? ExpectedHours { get; set; }

        [Column("status")]
        public RequestStatus Status { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("StudentId")]
        public virtual User Student { get; set; } = null!;

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        public virtual ICollection<Proposal> Proposals { get; set; } = new List<Proposal>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    }
}
