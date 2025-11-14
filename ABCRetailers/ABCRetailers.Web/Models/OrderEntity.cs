using System;

namespace ABCRetailers.Web.Models
{
    /// <summary>
    /// Entity that represents an order stored in Azure Storage.
    /// </summary>
    public class OrderEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // Who placed the order
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;

        // Which product
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        // Total = Product Price * Quantity (we’ll calculate this in service/controller)
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Order status used for logic like "only delete when shipped".
        /// Recommended values: "Pending", "Shipped", "Delivered", "Cancelled".
        /// </summary>
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ShippedAt { get; set; }
    }
}
