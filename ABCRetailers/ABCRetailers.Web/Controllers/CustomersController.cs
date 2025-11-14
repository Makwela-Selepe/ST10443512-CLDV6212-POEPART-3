using System;
using System.Linq;
using System.Threading.Tasks;
using ABCRetailers.Web.Models;
using ABCRetailers.Web.Models.ViewModels;
using ABCRetailers.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Web.Infrastructure;   // for GetObject / SetObject

namespace ABCRetailers.Web.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly AzureStorageService _storage;

        public CustomersController(AzureStorageService storage)
        {
            _storage = storage;
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Dashboard()
        {
            var username = User.Identity?.Name ?? string.Empty;

            var orders = await _storage.GetOrdersAsync();

            var myOrders = orders
                .Where(o => !string.IsNullOrEmpty(o.CustomerName) &&
                            o.CustomerName.Equals(username, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var cart = HttpContext.Session.GetObject<CartViewModel>("CART") ?? new CartViewModel();
            var cartItems = cart.Items.Sum(i => i.Quantity);

            var pending = myOrders.Count(o =>
                string.Equals(o.Status, "Pending", StringComparison.OrdinalIgnoreCase));

            var shipped = myOrders.Count(o =>
                string.Equals(o.Status, "Shipped", StringComparison.OrdinalIgnoreCase));   // ✅ NEW

            var delivered = myOrders.Count(o =>
                string.Equals(o.Status, "Delivered", StringComparison.OrdinalIgnoreCase));

            var vm = new CustomerDashboardViewModel
            {
                CustomerName = username,
                CartItems = cartItems,
                TotalOrders = myOrders.Count,
                PendingOrders = pending,
                ShippedOrders = shipped,          // ✅ NEW
                DeliveredOrders = delivered
            };

            return View(vm);
        }



        // GET: /Customers
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var entities = await _storage.GetCustomersAsync();
            var model = entities.Select(c => new CustomerViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                Phone = c.Phone,
                DeliveryAddress = c.DeliveryAddress
            }).ToList();

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var c = await _storage.GetCustomerByIdAsync(id);
            if (c == null) return NotFound();

            var vm = new CustomerViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                Phone = c.Phone,
                DeliveryAddress = c.DeliveryAddress
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, CustomerViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            if (id != model.Id) return BadRequest();

            var entity = new CustomerEntity
            {
                Id = model.Id!,
                Name = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                DeliveryAddress = model.DeliveryAddress
            };

            await _storage.AddOrUpdateCustomerAsync(entity);
            TempData["SuccessMessage"] = "Customer updated.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var c = await _storage.GetCustomerByIdAsync(id);
            if (c == null) return NotFound();

            var vm = new CustomerViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                Phone = c.Phone,
                DeliveryAddress = c.DeliveryAddress
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var ok = await _storage.DeleteCustomerAsync(id);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] =
                ok ? "Customer deleted." : "Customer not found.";
            return RedirectToAction(nameof(Index));
        }


        // GET: /Customers/Create
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new CustomerViewModel());
        }

        // POST: /Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CustomerViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var entity = new CustomerEntity
            {
                Id = model.Id ?? Guid.NewGuid().ToString(),
                Name = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                DeliveryAddress = model.DeliveryAddress
            };

            await _storage.AddOrUpdateCustomerAsync(entity);

            TempData["SuccessMessage"] = "Customer saved.";
            return RedirectToAction(nameof(Index));
        }
    }
}
