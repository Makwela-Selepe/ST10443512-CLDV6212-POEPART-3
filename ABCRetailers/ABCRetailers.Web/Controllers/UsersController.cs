using ABCRetailers.Web.Data;
using ABCRetailers.Web.Models;
using ABCRetailers.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly AuthDbContext _db;
        private readonly ILogger<UsersController> _logger;

        public UsersController(AuthDbContext db, ILogger<UsersController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET: /Users
        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _db.Users
                    .OrderBy(u => u.Role)
                    .ThenBy(u => u.Username)
                    .ToListAsync();

                ViewBag.TotalUsers = users.Count;
                ViewBag.AdminCount = users.Count(u => u.Role == "Admin");
                ViewBag.CustomerCount = users.Count(u => u.Role == "Customer");
                ViewBag.ActiveUsers = users.Count(u => u.IsActive);

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users");
                TempData["Error"] = "Error loading users. Please try again.";
                return View(new List<User>());
            }
        }

        // GET: /Users/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var user = await _db.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                // Get user statistics
                var cartItemsCount = await _db.Cart
                    .Where(c => c.CustomerUsername == user.Username)
                    .SumAsync(c => (int?)c.Quantity) ?? 0;

                ViewBag.CartItemsCount = cartItemsCount;

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user details for ID {UserId}", id);
                TempData["Error"] = "Error loading user details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Users/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            var vm = new EditUserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                Email = user.Email ?? "",
                DeliveryAddress = user.DeliveryAddress,
                Phone = user.Phone,
                Role = user.Role,
                IsActive = user.IsActive
            };

            return View(vm);
        }

        // POST: /Users/Edit/3
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditUserViewModel model)
        {
            if (id != model.Id)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                // Show validation errors
                return View(model);
            }

            var user = await _db.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            // Map edited fields back to the entity
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.Phone = model.Phone;
            user.DeliveryAddress = model.DeliveryAddress;
            user.Role = model.Role;
            user.IsActive = model.IsActive;

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "User updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Users/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _db.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                // Prevent deleting your own account
                if (user.Username == User.Identity!.Name)
                {
                    TempData["Error"] = "You cannot delete your own user account.";
                    return RedirectToAction(nameof(Index));
                }

                _db.Users.Remove(user);
                await _db.SaveChangesAsync();

                TempData["Success"] = $"User {user.Username} deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user ID {UserId}", id);
                TempData["Error"] = "Error deleting user. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Users/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var user = await _db.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                // Prevent deactivating your own account
                if (user.Username == User.Identity!.Name)
                {
                    TempData["Error"] = "You cannot deactivate your own user account.";
                    return RedirectToAction(nameof(Index));
                }

                user.IsActive = !user.IsActive;
                await _db.SaveChangesAsync();

                var status = user.IsActive ? "activated" : "deactivated";
                TempData["Success"] = $"User {user.Username} {status} successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status ID {UserId}", id);
                TempData["Error"] = "Error updating user status. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}