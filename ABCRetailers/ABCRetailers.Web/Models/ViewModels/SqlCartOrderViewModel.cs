// ABCRetailers.Web/Models/ViewModels/SqlCartOrderViewModel.cs
namespace ABCRetailers.Web.Models.ViewModels
{
    public class SqlCartOrderViewModel
    {
        public int Id { get; set; }

        public string CustomerUsername { get; set; } = string.Empty;
        public string? CustomerName { get; set; }      // optional, for display

        public string ProductId { get; set; } = string.Empty;
        public string? ProductName { get; set; }       // optional, for display

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }         // price of 1 item
        public decimal TotalAmount { get; set; }       // UnitPrice * Quantity

        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }        // when row was created
        public DateTime? ShippedAt { get; set; }       // null until shipped
    }
}
