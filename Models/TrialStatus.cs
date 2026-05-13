using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LAM_App.Models
{
    [Table("trial_statuses")]
    public class TrialStatus
    {
        [Key]
        [Column("status_id")]
        public int Id { get; set; }

        [Required]
        [Column("status_name")]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Column("to_comment")]
        public string? Comment { get; set; }

        public ICollection<TrialRecord> TrialRecords { get; set; } = new List<TrialRecord>();
    }
}
