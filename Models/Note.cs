using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LAM_App.Models
{
    [Table("notes")]
    public class Note
    {
        [Key]
        [Column("note_id")]
        public int Id { get; set; }

        [Column("note_date")]
        public DateTime NoteDate { get; set; } = DateTime.Today;

        [Column("status")]
        [MaxLength(50)]
        public string Status { get; set; } = "Задача";

        [Column("comment")]
        public string Comment { get; set; } = "";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
