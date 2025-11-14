using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Web.Services;

namespace ABCRetailers.Web.Controllers
{
    public class StorageController : Controller
    {
        private readonly AzureStorageService _azureStorageService;

        public StorageController(AzureStorageService azureStorageService)
        {
            _azureStorageService = azureStorageService;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _azureStorageService.GetProductsAsync();
            var orders = await _azureStorageService.GetOrdersAsync();
            var customers = await _azureStorageService.GetCustomersAsync();

            ViewBag.TotalProducts = products.Count;
            ViewBag.TotalOrders = orders.Count;
            ViewBag.TotalCustomers = customers.Count;

            return View();
        }
    }
}
