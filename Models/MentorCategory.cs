using System.ComponentModel.DataAnnotations.Schema;

namespace MentorEdu.Models
{
    [Table("mentor_categories")]
    public class MentorCategory
    {
        [Column("mentor_id")]
        public long MentorId { get; set; }

        [Column("category_id")]
        public long CategoryId { get; set; }

        // Navigation Properties
        [ForeignKey("MentorId")]
        public virtual User Mentor { get; set; } = null!;

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; } = null!;
    }
}
