using System.Threading.Tasks;
using ABCRetailers.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Web.Controllers
{
    public class ShopController : Controller
    {
        private readonly AzureStorageService _storage;

        public ShopController(AzureStorageService storage)
        {
            _storage = storage;
        }

        // GET: /Shop
        public async Task<IActionResult> Index()
        {
            var products = await _storage.GetProductsAsync();
            return View(products);
        }
    }
}
