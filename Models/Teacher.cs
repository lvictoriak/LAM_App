using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LAM_App.Models
{
    [Table("teachers")]
    public class Teacher
    {
        [Key]
        [Column("teacher_id")]
        public int Id { get; set; }

        [ForeignKey("Studio")]
        [Column("studio_id")]
        public int StudioId { get; set; }
        public Studio? Studio { get; set; }

        [Required]
        [Column("full_name")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Column("phone")]
        [MaxLength(20)]
        public string? Phone { get; set; }

        [Column("age")]
        public int? Age { get; set; }

        [Column("email")]
        [MaxLength(100)]
        public string? Email { get; set; }

        [Column("dance_experience")]
        public string? DanceExperience { get; set; }

        [Column("to_comment")]
        public string? Comment { get; set; }

        [Column("date_birth")]
        public DateTime? BirthDate { get; set; }

        public ICollection<TrialRecord> TrialRecords { get; set; } = new List<TrialRecord>();
    }
}
