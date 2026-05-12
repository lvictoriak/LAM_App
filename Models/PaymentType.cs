using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LAM_App.Models
{
    [Table("payment_types")]
    public class PaymentType
    {
        [Key]
        [Column("payment_type_id")]
        public int Id { get; set; }

        [Required]
        [Column("type_name")]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        // Связь: у вида оплаты много логов платежей
        public ICollection<PaymentLog> PaymentLogs { get; set; } = new List<PaymentLog>();
    }
}
