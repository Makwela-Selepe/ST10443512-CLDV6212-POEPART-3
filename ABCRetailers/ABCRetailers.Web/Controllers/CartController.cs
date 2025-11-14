using System.Linq;
using System.Threading.Tasks;
using ABCRetailers.Web.Data;
using ABCRetailers.Web.Infrastructure;
using ABCRetailers.Web.Models;
using ABCRetailers.Web.Models.ViewModels;
using ABCRetailers.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers.Web.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CartController : Controller
    {
        private readonly AzureStorageService _storage;
        private readonly AuthDbContext _db;
        private const string CartSessionKey = "CART";

        public CartController(AzureStorageService storage, AuthDbContext db)
        {
            _storage = storage;
            _db = db;
        }

        private CartViewModel GetCart()
        {
            var cart = HttpContext.Session.GetObject<CartViewModel>(CartSessionKey);
            return cart ?? new CartViewModel();
        }

        private void SaveCart(CartViewModel cart)
        {
            HttpContext.Session.SetObject(CartSessionKey, cart);
        }

        // GET: /Cart
        [HttpGet]
        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        // POST: /Cart/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(string productId, int quantity = 1)
        {
            if (string.IsNullOrWhiteSpace(productId))
                return RedirectToAction("Index", "Shop");

            var product = await _storage.GetProductAsync(productId); // RowKey
            if (product == null)
                return RedirectToAction("Index", "Shop");

            // session cart
            var cart = GetCart();

            var existing = cart.Items.FirstOrDefault(i => i.ProductId == product.RowKey);
            if (existing == null)
            {
                cart.Items.Add(new CartItemViewModel
                {
                    ProductId = product.RowKey!,
                    ProductName = product.ProductName,
                    UnitPrice = product.Price,
                    Quantity = quantity
                });
            }
            else
            {
                existing.Quantity += quantity;
            }

            SaveCart(cart);

            // ===== ALSO WRITE TO SQL CART TABLE =====
            var username = User.Identity?.Name ?? "guest";

            var existingRow = await _db.Cart
                .FirstOrDefaultAsync(c => c.CustomerUsername == username
                                       && c.ProductId == product.RowKey);

            if (existingRow == null)
            {
                _db.Cart.Add(new Cart
                {
                    CustomerUsername = username,
                    ProductId = product.RowKey!,
                    Quantity = quantity,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existingRow.Quantity += quantity;
            }

            await _db.SaveChangesAsync();
            // ========================================

            TempData["SuccessMessage"] = "Item added to cart.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(string productId)
        {
            var cart = GetCart();
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                cart.Items.Remove(item);
                SaveCart(cart);
                TempData["SuccessMessage"] = "Item removed.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/Clear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CartSessionKey);
            TempData["SuccessMessage"] = "Cart cleared.";
            return RedirectToAction(nameof(Index));
        }


      
        // POST: /Cart/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCart();
            if (!cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToAction(nameof(Index));
            }

            // identify the customer (adjust to your own user system if needed)
            var customerName = User.Identity?.Name ?? "Guest";
            var customerId = customerName; // or some other unique ID if you have one

            // turn each cart item into an order
            foreach (var item in cart.Items)
            {
                var order = new OrderEntity
                {
                    Id = Guid.NewGuid().ToString("N"),
                    CustomerId = customerId,
                    CustomerName = customerName,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    TotalAmount = item.UnitPrice * item.Quantity,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                await _storage.CreateOrderAsync(order);   // ✅ use _storage, not _azureStorageService
            }

            // clear the cart after successful checkout
            HttpContext.Session.Remove(CartSessionKey);

            TempData["SuccessMessage"] = "Order placed successfully.";

            return RedirectToAction(
                "Index",
                "MyOrders",
                new { customerId = customerId, customerName = customerName });
        }

    }
}
