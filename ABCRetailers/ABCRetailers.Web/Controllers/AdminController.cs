using System;
using System.Linq;
using System.Threading.Tasks;
using ABCRetailers.Web.Data;
using ABCRetailers.Web.Models.ViewModels;
using ABCRetailers.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AzureStorageService _azureStorageService;
        private readonly AuthDbContext _db;

        public AdminController(AzureStorageService azureStorageService, AuthDbContext db)
        {
            _azureStorageService = azureStorageService;
            _db = db;
        }

        // ========== DASHBOARD ==========

        // Landing page after admin login
        public async Task<IActionResult> Index()
        {
            var customers = await _azureStorageService.GetCustomersAsync();
            var orders = await _azureStorageService.GetOrdersAsync();
            var products = await _azureStorageService.GetProductsAsync();
            var proofs = await _azureStorageService.GetPaymentProofsAsync();

            // Counts used by the dashboard view
            ViewBag.CustomerCount = customers.Count;
            ViewBag.ProductCount = products.Count;
            ViewBag.TotalOrders = orders.Count;
            ViewBag.PendingOrders = orders.Count(o =>
                string.Equals(o.Status, "Pending", StringComparison.OrdinalIgnoreCase));
            ViewBag.PaymentProofsCount = proofs.Count;

            // Optional – if you don’t calculate these yet, just leave them 0
            ViewBag.TotalUsers = 0;
            ViewBag.AdminCount = 0;

            return View();
        }

        public IActionResult Products()
        {
            return RedirectToAction("Index", "Products");
        }

        // ========== PRODUCTS ==========

        [HttpGet]
        public IActionResult CreateProduct()
        {
            return View(new ProductViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(ProductViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Optional image upload
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploadResult = await _azureStorageService.UploadProductImageAsync(model.ImageFile);
                if (uploadResult.Ok)
                {
                    model.ImageBlobName = uploadResult.BlobName;
                    model.ImageUrl = uploadResult.BlobUri;
                }
            }

            // use entered Quantity as initial stock
            model.StockAvailable = model.Quantity;

            var ok = await _azureStorageService.CreateProductAsync(model);
            if (!ok)
            {
                ModelState.AddModelError("", "Failed to save product.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Product created successfully.";
            return RedirectToAction(nameof(Products));
        }

        // ========== ORDERS (SQL CART) ==========

        public async Task<IActionResult> Orders()
        {
            // 1) Get all SQL cart rows (your local "orders" table)
            var rows = await _db.Cart
                .AsNoTracking()
                .OrderBy(c => c.CustomerUsername)
                .ThenBy(c => c.Id)
                .ToListAsync();

            // 2) Load all products once from Azure Table Storage
            var products = await _azureStorageService.GetProductsAsync();

            // 3) Join SQL rows to products so we can show names + prices
            var model =
                (from c in rows
                 join p in products
                     on c.ProductId equals p.RowKey      // Cart.ProductId is the product RowKey
                 select new SqlCartOrderViewModel
                 {
                     Id = c.Id,
                     CustomerUsername = c.CustomerUsername,
                     CustomerName = c.CustomerUsername,   // or map to real name if you store it

                     ProductId = c.ProductId,
                     ProductName = p.ProductName,         // adjust if your property is just Name

                     Quantity = c.Quantity,
                     UnitPrice = p.Price,
                     TotalAmount = p.Price * c.Quantity,

                     Status = c.Status,
                     CreatedAt = c.CreatedAt,
                     ShippedAt = c.ShippedAt
                 })
                .ToList();

            return View(model);
        }

        // Update status on the SQL Cart row
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                TempData["ErrorMessage"] = "Invalid status.";
                return RedirectToAction(nameof(Orders));
            }

            var order = await _db.Cart.FirstOrDefaultAsync(c => c.Id == id);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToAction(nameof(Orders));
            }

            order.Status = status;

            // Stamp shipped date when first becoming Shipped or Delivered
            if (status == "Shipped" || status == "Delivered")
            {
                if (!order.ShippedAt.HasValue)
                    order.ShippedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Order marked as {status}.";
            return RedirectToAction(nameof(Orders));
        }

        // Delete order only when shipped (or delivered)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _db.Cart.FirstOrDefaultAsync(c => c.Id == id);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToAction(nameof(Orders));
            }

            if (order.Status != "Shipped" && order.Status != "Delivered")
            {
                TempData["ErrorMessage"] =
                    "Order can only be deleted after it has been marked as 'Shipped'.";
                return RedirectToAction(nameof(Orders));
            }

            _db.Cart.Remove(order);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Order deleted successfully.";
            return RedirectToAction(nameof(Orders));
        }
    }
}
