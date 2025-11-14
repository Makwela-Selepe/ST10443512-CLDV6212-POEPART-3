using System.Threading.Tasks;
using ABCRetailers.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly AzureStorageService _storage;

        public OrdersController(AzureStorageService storage)
        {
            _storage = storage;
        }

        // GET: /Orders
        public async Task<IActionResult> Index()
        {
            var orders = await _storage.GetAllOrdersAsync();
            return View(orders);
        }

        // POST: /Orders/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var order = await _storage.GetOrderAsync(partitionKey, rowKey);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToAction(nameof(Index));
            }

            // 🚫 ONLY SHIPPED ORDERS CAN BE DELETED
            if (!string.Equals(order.Status, "Shipped", System.StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Only shipped orders can be deleted.";
                return RedirectToAction(nameof(Index));
            }

            await _storage.DeleteOrderAsync(partitionKey, rowKey);
            TempData["SuccessMessage"] = "Order deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
