using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LAM_App.Models
{
    [Table("styles")]
    public class Style
    {
        [Key]
        [Column("style_id")]
        public int Id { get; set; }

        [ForeignKey("Studio")]
        [Column("studio_id")]
        public int StudioId { get; set; }
        public Studio? Studio { get; set; }

        [Required]
        [Column("style_name")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column("schedule_options")]
        public string? ScheduleOptions { get; set; }

        [Column("teacher")]
        public int? TeacherId { get; set; }

        [Column("to_comment")]
        public string? Comment { get; set; }

        public ICollection<TrialRecord> TrialRecords { get; set; } = new List<TrialRecord>();

        public ICollection<Client> Clients { get; set; } = new List<Client>();

        public ICollection<PaymentLog> PaymentLogs { get; set; } = new List<PaymentLog>();
    }
}
