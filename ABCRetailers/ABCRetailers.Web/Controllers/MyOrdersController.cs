using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Web.Models.ViewModels;
using ABCRetailers.Web.Models;      // ✅ this is needed for OrderViewModel
using ABCRetailers.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Web.Controllers
{
    public class MyOrdersController : Controller
    {
        private readonly AzureStorageService _azureStorageService;

        public MyOrdersController(AzureStorageService azureStorageService)
        {
            _azureStorageService = azureStorageService;
        }

        // /MyOrders?customerId=... OR ?customerName=...
        [HttpGet]
        public async Task<IActionResult> Index(string? customerId, string? customerName)
        {
            var allOrders = await _azureStorageService.GetOrdersAsync();

            var filtered = allOrders.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(customerId))
            {
                filtered = filtered.Where(o => o.CustomerId == customerId);
            }
            else if (!string.IsNullOrWhiteSpace(customerName))
            {
                filtered = filtered.Where(o => o.CustomerName == customerName);
            }

            var model = filtered.Select(o => new OrderViewModel
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
                CustomerName = o.CustomerName,
                ProductId = o.ProductId,
                ProductName = o.ProductName,
                Quantity = o.Quantity,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
                ShippedAt = o.ShippedAt
            }).ToList();

            return View(model);
        }
    }
}
