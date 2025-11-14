using System.Collections.Generic;
using System.Linq;

namespace ABCRetailers.Web.Models.ViewModels
{
    public class CartItemViewModel
    {
        public string ProductId { get; set; } = string.Empty;   // use Product RowKey
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        public decimal LineTotal => UnitPrice * Quantity;
    }

    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();

        public decimal Total => Items.Sum(i => i.LineTotal);
    }
}
