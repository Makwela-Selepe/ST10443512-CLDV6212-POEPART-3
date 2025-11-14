using Azure;
using Azure.Data.Tables;

namespace ABCRetailers.Functions.Models
{
    // Must match the "Products" table schema used by the MVC app
    public class ProductEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "PRODUCTS";  // same as Web
        public string RowKey { get; set; } = default!;          // = Id/RowKey
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public int ProductId { get; set; }                      // auto-increment
        public string ProductName { get; set; } = "";
        public string? Description { get; set; } = "";
        public decimal Price { get; set; }
        public int StockAvailable { get; set; }

        public string? ImageBlobName { get; set; }              // stored in table
        public string? ImageUrl { get; set; }                   // filled by functions
    }
}
