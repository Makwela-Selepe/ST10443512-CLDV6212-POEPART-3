using ABCRetailers.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Web.Controllers;

public class HomeController : Controller
{
    private readonly FunctionsApi _api;
    private readonly IConfiguration _cfg;

    public HomeController(FunctionsApi api, IConfiguration cfg)
    {
        _api = api;
        _cfg = cfg;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        // If user is authenticated, redirect to appropriate dashboard
        if (User.Identity.IsAuthenticated)
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Index", "Admin");
            else if (User.IsInRole("Customer"))
                return RedirectToAction("Dashboard", "Customers");
        }

        // Show public home page for unauthenticated users
        try
        {
            var products = await _api.GetProductsAsync() ?? new();
            ViewBag.ProductCount = products.Count;

            var customers = await _api.GetCustomersAsync() ?? new();
            ViewBag.CustomerCount = customers.Count;

            var orders = await _api.GetOrdersAsync() ?? new();
            ViewBag.OrderCount = orders.Count;

            ViewBag.Featured = products.Take(4).ToList();
            ViewBag.ProductImagesBaseUrl = _cfg["Storage:ProductImagesBaseUrl"]?.TrimEnd('/');
        }
        catch (Exception ex)
        {
            // If API calls fail, set defaults
            ViewBag.ProductCount = 0;
            ViewBag.CustomerCount = 0;
            ViewBag.OrderCount = 0;
            ViewBag.Featured = new List<object>();
        }

        return View();
    }

    [AllowAnonymous]
    public IActionResult Privacy() => View();
}