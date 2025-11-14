using Azure;
using Azure.Data.Tables;

namespace ABCRetailers.Functions.Models
{
    // Must match MVC OrderEntity schema (table "Orders")
    public class OrderEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "ORDER";
        public string RowKey { get; set; } = Guid.NewGuid().ToString("N");
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;

        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ShippedAt { get; set; }
    }
}
