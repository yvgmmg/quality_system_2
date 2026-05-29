using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityControlSystem.Infrastructure.Entities
{
    [Table("requisition-invoice")]
    public class RequisitionInvoice
    {
        [Key]
        [Column("requisition-invoice_id")]
        public int RequisitionInvoiceId { get; set; }

        [Column("production_order_id")]
        public int? ProductionOrderId { get; set; }
        public virtual ProductionOrder? ProductionOrder { get; set; }

        [Column("creation_date")]
        public DateTime CreationDate { get; set; }
    }
}
