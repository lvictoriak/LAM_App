using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LAM_App.Models
{
    [Table("attendance_subscriptions")]
    public class AttendanceSubscription
    {
        [Key]
        [Column("subscription_id")]
        public int Id { get; set; }

        [ForeignKey("Client")]
        [Column("client_id")]
        public int ClientId { get; set; }
        public Client? Client { get; set; }

        [ForeignKey("Style")]
        [Column("style_id")]
        public int StyleId { get; set; }
        public Style? Style { get; set; }

        [Column("total_lessons")]
        public int TotalLessons { get; set; }

        [Column("used_lessons")]
        public int UsedLessons { get; set; }

        [Column("start_date")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        [Column("finished_at")]
        public DateTime? FinishedAt { get; set; }

        [Column("is_paid")]
        public bool IsPaid { get; set; }

        [ForeignKey("Payment")]
        [Column("payment_id")]
        public int? PaymentId { get; set; }
        public PaymentLog? Payment { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<AttendanceMark> Marks { get; set; } = new List<AttendanceMark>();
    }
}
