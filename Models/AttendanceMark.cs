using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LAM_App.Models
{
    [Table("attendance_marks")]
    public class AttendanceMark
    {
        [Key]
        [Column("mark_id")]
        public int Id { get; set; }

        [ForeignKey("Session")]
        [Column("session_id")]
        public int SessionId { get; set; }
        public AttendanceSession? Session { get; set; }

        [ForeignKey("Client")]
        [Column("client_id")]
        public int ClientId { get; set; }
        public Client? Client { get; set; }

        [ForeignKey("Subscription")]
        [Column("subscription_id")]
        public int? SubscriptionId { get; set; }
        public AttendanceSubscription? Subscription { get; set; }

        [Column("lesson_number")]
        public int? LessonNumber { get; set; }

        [Column("is_absent")]
        public bool IsAbsent { get; set; }

        [Column("is_medical_excused")]
        public bool IsMedicalExcused { get; set; }
    }
}
