using ABCRetailers.Web.Data;
using ABCRetailers.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers.Web.Controllers
{
    public class DatabaseController : Controller
    {
        private readonly AuthDbContext _db;

        public DatabaseController(AuthDbContext db)
        {
            _db = db;
        }

        [HttpGet("/db/check")]
        public async Task<IActionResult> CheckDatabase()
        {
            try
            {
                // Test database connection
                var canConnect = await _db.Database.CanConnectAsync();

                // Get user count
                var userCount = await _db.Users.CountAsync();

                // Get all users
                var users = await _db.Users.ToListAsync();

                var result = new
                {
                    DatabaseConnected = canConnect,
                    UserCount = userCount,
                    Users = users.Select(u => new { u.Id, u.Username, u.Role, u.Email, u.IsActive }),
                    Message = canConnect ? "Database connection successful!" : "Database connection failed!"
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    DatabaseConnected = false,
                    Error = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet("/db/reset")]
        public async Task<IActionResult> ResetDatabase()
        {
            try
            {
                // Clear existing data
                _db.Users.RemoveRange(_db.Users);
                await _db.SaveChangesAsync();

                // Add default admin and customer
                var defaultUsers = new[]
                {
                    new User
                    {
                        Username = "admin",
                        PasswordHash = "admin123",
                        Role = "Admin",
                        Email = "admin@abcretailers.com",
                        FirstName = "System",
                        LastName = "Administrator",
                        Phone = "+1-555-0100",
                        IsActive = true
                    },
                    new User
                    {
                        Username = "customer",
                        PasswordHash = "customer123",
                        Role = "Customer",
                        Email = "customer@abcretailers.com",
                        FirstName = "John",
                        LastName = "Smith",
                        Phone = "+1-555-0101",
                        DeliveryAddress = "123 Main Street, New York, NY 10001",
                        IsActive = true
                    }
                };

                await _db.Users.AddRangeAsync(defaultUsers);
                await _db.SaveChangesAsync();

                return Content(@"
                    <html>
                    <body>
                        <h1>Database Reset Successfully!</h1>
                        <p>Default users created:</p>
                        <ul>
                            <li><strong>Admin:</strong> admin / admin123</li>
                            <li><strong>Customer:</strong> customer / customer123</li>
                        </ul>
                        <a href='/Account/Login'>Go to Login</a>
                    </body>
                    </html>", "text/html");
            }
            catch (Exception ex)
            {
                return Content($"<h1>Database Reset Failed!</h1><p>{ex.Message}</p>", "text/html");
            }
        }
    }
}