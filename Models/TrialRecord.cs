using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LAM_App.Models
{
    [Table("trial_records")]
    public class TrialRecord
    {
        [Key]
        [Column("trial_id")]
        public int Id { get; set; }

        [ForeignKey("TrialStatus")]
        [Column("status_id")]
        public int StatusId { get; set; }
        public TrialStatus? Status { get; set; }

        [ForeignKey("Style")]
        [Column("style_id")]
        public int StyleId { get; set; }
        public Style? Style { get; set; }

        [ForeignKey("Teacher")]
        [Column("instructor_id")]
        public int InstructorId { get; set; }
        public Teacher? Instructor { get; set; }

        [Required]
        [Column("parent_name")]
        [MaxLength(150)]
        public string ParentName { get; set; } = string.Empty;

        [Required]
        [Column("parent_phone")]
        [MaxLength(20)]
        public string ParentPhone { get; set; } = string.Empty;

        [Required]
        [Column("child_name")]
        [MaxLength(150)]
        public string ChildName { get; set; } = string.Empty;

        [Column("child_age")]
        public int? ChildAge { get; set; }

        [Column("trial_date")]
        public DateTime TrialDate { get; set; }

        [Column("record_date")]
        public DateTime RecordDate { get; set; } = DateTime.Now;

        [Column("to_comment")]
        public string? Comment { get; set; }

    }
}