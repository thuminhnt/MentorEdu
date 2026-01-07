using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentorEdu.Models
{
    [Table("categories")]
    public class Category
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [MaxLength(255)]
        [Column("name")]
        public string? Name { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("parent_id")]
        public long? ParentId { get; set; }

        // Navigation Properties
        [ForeignKey("ParentId")]
        public virtual Category? Parent { get; set; }

        public virtual ICollection<Category> Children { get; set; } = new List<Category>();
        public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
        public virtual ICollection<MentorCategory> MentorCategories { get; set; } = new List<MentorCategory>();
    }
}
