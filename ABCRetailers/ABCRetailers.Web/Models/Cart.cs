using System;

namespace ABCRetailers.Web.Models
{
    public class Cart
    {
        public int Id { get; set; }

        // who owns this cart row
        public string CustomerUsername { get; set; } = string.Empty;

        // RowKey from Product table (string)
        public string ProductId { get; set; } = string.Empty;

        public int Quantity { get; set; }

        // when the row was created
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ===== NEW FIELDS for admin Orders page =====

        // Pending / Processed / Shipped / Delivered
        public string Status { get; set; } = "Pending";

        // when it was first shipped (or delivered)
        public DateTime? ShippedAt { get; set; }
    }
}
