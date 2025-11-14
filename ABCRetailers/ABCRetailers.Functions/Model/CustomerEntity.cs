using Azure;
using Azure.Data.Tables;

namespace ABCRetailers.Functions.Models
{
    // Make this match the "Customers" table the MVC app expects
    public class CustomerEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "CUSTOMER";
        public string RowKey { get; set; } = Guid.NewGuid().ToString("N");
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // IMPORTANT: These names must match what Web AzureStorageService writes/reads
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
    }
}
