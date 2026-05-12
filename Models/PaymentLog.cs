using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LAM_App.Models
{
    [Table("payment_log")]
    public class PaymentLog
    {
        [Key]
        [Column("payment_id")]
        public int Id { get; set; }

        [Column("payment_date")]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Column("income", TypeName = "numeric(10,2)")]
        public decimal? Income { get; set; }

        [Column("expense", TypeName = "numeric(10,2)")]
        public decimal? Expense { get; set; }

        [ForeignKey("PaymentType")]
        [Column("payment_type_id")]
        public int? PaymentTypeId { get; set; }
        public PaymentType? PaymentType { get; set; }

        [ForeignKey("IncomeCategory")]
        [Column("category_id")]
        public int? CategoryId { get; set; }
        public IncomeCategory? IncomeCategory { get; set; }

        [ForeignKey("Style")]
        [Column("style_id")]
        public int? StyleId { get; set; }
        public Style? Style { get; set; }

        [Column("contractor")]
        [MaxLength(150)]
        public string? Contractor { get; set; }

        [Column("to_comment")]
        public string? Comment { get; set; }

        [Column("extra_info")]
        public string? ExtraInfo { get; set; }
    }
}
