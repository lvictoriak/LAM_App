using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Windows;

namespace LAM_App.Models
{
    [Table("studio")]
    public class Studio
    {
        [Key]
        [Column("studio_id")]
        public int Id { get; set; }

        [Required]
        [Column("studio_name")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column("address")]
        [MaxLength(255)]
        public string? Address { get; set; }

        [Column("phone")]
        [MaxLength(20)]
        public string? Phone { get; set; }

        // Связь: у одной студии много преподавателей
        public ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();

        // Связь: у одной студии много направлений
        public ICollection<Style> Styles { get; set; } = new List<Style>();
    }
}
