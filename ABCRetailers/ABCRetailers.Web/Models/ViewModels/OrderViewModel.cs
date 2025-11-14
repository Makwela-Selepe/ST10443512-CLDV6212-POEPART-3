using System;
using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Web.Models.ViewModels
{
    public class OrderViewModel
    {
        public string? Id { get; set; }
        public string? CustomerId { get; set; }

        [Display(Name = "Customer")]
        public string? CustomerName { get; set; }

        public string? ProductId { get; set; }

        [Display(Name = "Product")]
        public string? ProductName { get; set; }

        [Range(1, 100000)]
        public int Quantity { get; set; }

        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ShippedAt { get; set; }
    }
}
