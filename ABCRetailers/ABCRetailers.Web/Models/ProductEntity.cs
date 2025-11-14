using System;

namespace ABCRetailers.Web.Models
{
    /// <summary>
    /// Entity that we store in Azure Storage for products.
    /// This can be mapped to a table row or a blob metadata record.
    /// </summary>
    public class ProductEntity
    {
        // This will be our internal id (we can map it to RowKey in storage).
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // Basic product fields
        public string ProductName { get; set; } = string.Empty;
        public string? Description { get; set; }

        // This is the amount you type – will be stored both in storage and used in the app
        public decimal Price { get; set; }

        // Optional stock quantity
        public int Quantity { get; set; }

        // Image information stored in storage
        // e.g. "products/12345.jpg"
        public string? ImageBlobName { get; set; }

        // Full URL to the image in blob storage so we can display it in the web app
        public string? ImageUrl { get; set; }
    }
}
