using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ABCRetailers.Web.Models.ViewModels
{
    public class ProductViewModel
    {
        // Azure Table Id / RowKey
        public string? Id { get; set; }          // used by services & controllers
        public string? RowKey { get; set; }      // also used as RowKey in Tables

        // Optional numeric ProductId (if you ever need an int id)
        public int? ProductId { get; set; }

        // ----------------- core product info -----------------
        [Required]
        [Display(Name = "Product name")]
        public string ProductName { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Range(0, 1_000_000)]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        // Quantity used in forms (e.g. when you create/edit product,
        // and for cart quantity)
        [Range(0, 1_000_000)]
        public int Quantity { get; set; }

        // Inventory / stock tracking (AzureStorageService maps to this)
        public int StockAvailable { get; set; }

        // ----------------- image info -----------------
        // Name of the blob in the product-images container
        public string? ImageBlobName { get; set; }

        // Public URL to the image (we often just build this from BlobName,
        // but some views use this directly)
        public string? ImageUrl { get; set; }

        // Used only when posting a form with a file input in the Admin UI
        [Display(Name = "Product image")]
        public IFormFile? ImageFile { get; set; }
    }
}
