using ABCRetailers.Web.Data;
using ABCRetailers.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ABCRetailers.Web.Models.ViewModels;

namespace ABCRetailers.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthDbContext _db;
        private readonly ILogger<AccountController> _logger;

        public AccountController(AuthDbContext db, ILogger<AccountController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ========== LOGIN ==========

        // GET: /Account/Login
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: /Account/Login
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Login attempt with invalid model");
                return View(model);
            }

            try
            {
                _logger.LogInformation("Login attempt for user: {Username}", model.Username);

                // Find user in database (case-insensitive) and must be active
                var user = await _db.Users
                    .FirstOrDefaultAsync(u =>
                        u.Username.ToLower() == model.Username.ToLower() &&
                        u.IsActive);

                if (user == null)
                {
                    _logger.LogWarning("Login failed - user not found: {Username}", model.Username);
                    ModelState.AddModelError("", "Invalid username or password.");
                    return View(model);
                }

                // Note: in real app use a password hasher!
                if (user.PasswordHash != model.Password)
                {
                    _logger.LogWarning("Login failed - invalid password for user: {Username}", model.Username);
                    ModelState.AddModelError("", "Invalid username or password.");
                    return View(model);
                }

                if (!string.Equals(user.Role, model.Role, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Login failed - role mismatch for user: {Username}. Expected: {UserRole}, Selected: {SelectedRole}",
                        model.Username, user.Role, model.Role);

                    ModelState.AddModelError("",
                        $"This account is registered as {user.Role}. Please select the correct role.");
                    return View(model);
                }

                _logger.LogInformation("Login successful for user: {Username} with role: {Role}",
                    user.Username, user.Role);

                // Update last login
                user.LastLogin = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                // Create auth cookie
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("UserId", user.Id.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Redirect by role
                if (user.Role == "Admin")
                {
                    _logger.LogInformation("Redirecting admin user to admin dashboard");
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    _logger.LogInformation("Redirecting customer user to customer dashboard");
                    return RedirectToAction("Dashboard", "Customers");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for user: {Username}", model.Username);
                ModelState.AddModelError("", $"Login failed: {ex.Message}");
                return View(model);
            }

        }

        // ========== REGISTER ==========

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            var model = new RegisterViewModel
            {
                Role = "Customer" // default selection
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Normalise role
            if (model.Role != "Admin" && model.Role != "Customer")
            {
                model.Role = "Customer";
            }

            // Delivery address required only for customers
            if (model.Role == "Customer" && string.IsNullOrWhiteSpace(model.DeliveryAddress))
            {
                ModelState.AddModelError(nameof(model.DeliveryAddress),
                    "Delivery address is required for customers.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check for existing username
            var existing = await _db.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username);

            if (existing != null)
            {
                ModelState.AddModelError(nameof(model.Username), "That username is already taken.");
                return View(model);
            }

            var user = new User
            {
                Username = model.Username,
                // TODO: replace with proper password hashing in a real app
                PasswordHash = model.Password,
                Role = model.Role,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Phone = model.Phone,
                DeliveryAddress = model.Role == "Customer"
                    ? model.DeliveryAddress
                    : null,
                DateCreated = DateTime.UtcNow,
                IsActive = true
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Auto-login or redirect to login, depending on how you had it before
            // For example:
            // await SignInUserAsync(user);
            // return RedirectToAction("Index", "Home");

            return RedirectToAction("Login", "Account");
        }

        // ========== LOGOUT / DENIED ==========

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User logged out");
            return RedirectToAction("Login");
        }

        public IActionResult Denied()
        {
            return View();
        }

        // ========== TEST / DEBUG HELPERS ==========

        [AllowAnonymous]
        [HttpGet("/test-login")]
        public IActionResult TestLogin()
        {
            return Content(@"
                <html>
                <head>
                    <title>Test Login - ABC Retailers</title>
                    <link href='https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.min.css' rel='stylesheet'>
                </head>
                <body>
                    <div class='container mt-4'>
                        <h1>Test Login Forms</h1>
                        
                        <div class='row'>
                            <div class='col-md-6'>
                                <div class='card'>
                                    <div class='card-header bg-danger text-white'>
                                        <h5>Admin Login</h5>
                                    </div>
                                    <div class='card-body'>
                                        <form method='post' action='/Account/Login'>
                                            <input type='hidden' name='Role' value='Admin'>
                                            <div class='mb-3'>
                                                <label class='form-label'>Username</label>
                                                <input type='text' name='Username' value='admin' class='form-control' required>
                                            </div>
                                            <div class='mb-3'>
                                                <label class='form-label'>Password</label>
                                                <input type='password' name='Password' value='admin123' class='form-control' required>
                                            </div>
                                            <button type='submit' class='btn btn-danger w-100'>Login as Admin</button>
                                        </form>
                                    </div>
                                </div>
                            </div>
                            
                            <div class='col-md-6'>
                                <div class='card'>
                                    <div class='card-header bg-primary text-white'>
                                        <h5>Customer Login</h5>
                                    </div>
                                    <div class='card-body'>
                                        <form method='post' action='/Account/Login'>
                                            <input type='hidden' name='Role' value='Customer'>
                                            <div class='mb-3'>
                                                <label class='form-label'>Username</label>
                                                <input type='text' name='Username' value='customer' class='form-control' required>
                                            </div>
                                            <div class='mb-3'>
                                                <label class='form-label'>Password</label>
                                                <input type='password' name='Password' value='customer123' class='form-control' required>
                                            </div>
                                            <button type='submit' class='btn btn-primary w-100'>Login as Customer</button>
                                        </form>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </body>
                </html>", "text/html");
        }

        [AllowAnonymous]
        [HttpGet("/debug-login")]
        public async Task<IActionResult> DebugLogin()
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine("<h1>Login Debug Information</h1>");

            try
            {
                var canConnect = await _db.Database.CanConnectAsync();
                result.AppendLine($"<h3>Database Connection Test:</h3>");
                result.AppendLine($"<p>Database Connected: <strong>{canConnect}</strong></p>");

                if (canConnect)
                {
                    var users = await _db.Users.ToListAsync();
                    result.AppendLine($"<p>Total Users: <strong>{users.Count}</strong></p>");

                    result.AppendLine("<h3>Existing Users:</h3>");
                    result.AppendLine("<table border='1'><tr><th>ID</th><th>Username</th><th>Password</th><th>Role</th><th>Active</th></tr>");
                    foreach (var user in users)
                    {
                        result.AppendLine($"<tr><td>{user.Id}</td><td>{user.Username}</td><td>{user.PasswordHash}</td><td>{user.Role}</td><td>{user.IsActive}</td></tr>");
                    }
                    result.AppendLine("</table>");
                }

                result.AppendLine("<h3>Test Credentials:</h3>");
                result.AppendLine("<p><strong>Admin:</strong> admin / admin123</p>");
                result.AppendLine("<p><strong>Customer:</strong> customer / customer123</p>");

                return Content(result.ToString(), "text/html");
            }
            catch (Exception ex)
            {
                result.AppendLine($"<p style='color: red;'><strong>ERROR:</strong> {ex.Message}</p>");
                result.AppendLine($"<pre>{ex.StackTrace}</pre>");
                return Content(result.ToString(), "text/html");
            }
        }
    }
}
