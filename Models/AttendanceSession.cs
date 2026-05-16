using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LAM_App.Models
{
    [Table("attendance_sessions")]
    public class AttendanceSession
    {
        [Key]
        [Column("session_id")]
        public int Id { get; set; }

        [ForeignKey("Style")]
        [Column("style_id")]
        public int StyleId { get; set; }
        public Style? Style { get; set; }

        [Column("session_date")]
        public DateTime SessionDate { get; set; }

        [Column("children_count")]
        public int ChildrenCount { get; set; }

        [ForeignKey("SubstituteTeacher")]
        [Column("substitute_teacher_id")]
        public int? SubstituteTeacherId { get; set; }
        public Teacher? SubstituteTeacher { get; set; }

        [Column("substitute_teacher_name")]
        [MaxLength(150)]
        public string? SubstituteTeacherName { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<AttendanceMark> Marks { get; set; } = new List<AttendanceMark>();
    }
}
