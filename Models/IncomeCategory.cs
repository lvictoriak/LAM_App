using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LAM_App.Models
{
    [Table("income_categories")]
    public class IncomeCategory
    {
        [Key]
        [Column("category_id")]
        public int Id { get; set; }

        [Required]
        [Column("category_name")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public ICollection<PaymentLog> PaymentLogs { get; set; } = new List<PaymentLog>();
    }
}
