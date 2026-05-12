using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LAM_App.Models
{
    [Table("clients")]
    public class Client
    {
        [Key]
        [Column("client_id")]
        public int Id { get; set; }

        [Required]
        [Column("parent_name")]
        [MaxLength(150)]
        public string ParentName { get; set; } = string.Empty;

        [Required]
        [Column("parent_phone")]
        [MaxLength(20)]
        public string ParentPhone { get; set; } = string.Empty;

        [Required]
        [Column("child_surname")]
        [MaxLength(100)]
        public string ChildSurname { get; set; } = string.Empty;

        [Required]
        [Column("child_name")]
        [MaxLength(100)]
        public string ChildName { get; set; } = string.Empty;

        [Column("child_patronymic")]
        [MaxLength(100)]
        public string? ChildPatronymic { get; set; }

        [Column("birth_date")]
        public DateTime? BirthDate { get; set; }

        [Column("age")]
        public int? Age { get; set; }

        [Column("shift")]
        [MaxLength(50)]
        public string? Shift { get; set; } // Учебная смена или группа

        [ForeignKey("Style")]
        [Column("style_name")] // Внимание: в диаграмме это integer, но названо style_name — возможно, ошибка именования
        public int? StyleId { get; set; }
        public Style? Style { get; set; }

        [Column("to_comment")]
        public string? Comment { get; set; }

        // Связь: у клиента много записей на пробные
        public ICollection<TrialRecord> TrialRecords { get; set; } = new List<TrialRecord>();
    }
}
