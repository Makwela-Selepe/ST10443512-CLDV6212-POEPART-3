using System;
using System.Threading.Tasks;
using ABCRetailers.Web.Models.ViewModels;   // ✅ ProductViewModel lives here
using ABCRetailers.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ABCRetailers.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly AzureStorageService _storageService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            AzureStorageService storageService,
            ILogger<ProductsController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        // GET: /Products
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _storageService.GetProductsAsync();
                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products");
                return View(new System.Collections.Generic.List<ProductViewModel>());
            }
        }

        // GET: /Products/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new ProductViewModel());
        }

        // POST: /Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductViewModel vm, IFormFile? image)
        {
            if (!ModelState.IsValid)
                return View(vm);

            try
            {
                // Ensure a RowKey / Id so table + blob can line up
                if (string.IsNullOrWhiteSpace(vm.RowKey))
                    vm.RowKey = Guid.NewGuid().ToString("N");

                vm.Id ??= vm.RowKey;

                // Optional image upload (uses the 1-arg method in AzureStorageService)
                if (image != null && image.Length > 0)
                {
                    var upload = await _storageService.UploadProductImageAsync(image);
                    if (upload.Ok)
                    {
                        vm.ImageBlobName = upload.BlobName;
                        vm.ImageUrl = upload.BlobUri;
                    }
                }

                // Use entered Quantity as initial stock
                vm.StockAvailable = vm.Quantity;

                var success = await _storageService.CreateProductAsync(vm);

                if (success)
                {
                    TempData["Success"] = "Product created successfully!";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", "Failed to create product.");
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                ModelState.AddModelError("", "Error creating product. Please try again.");
                return View(vm);
            }
        }

        // GET: /Products/Edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest();

            var model = await _storageService.GetProductAsync(id);
            if (model == null)
                return NotFound();

            return View(model);
        }

        // POST: /Products/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ProductViewModel vm, IFormFile? image)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest();

            if (!ModelState.IsValid)
                return View(vm);

            try
            {
                // Optional new image
                if (image != null && image.Length > 0)
                {
                    var upload = await _storageService.UploadProductImageAsync(image);
                    if (upload.Ok)
                    {
                        vm.ImageBlobName = upload.BlobName;
                        vm.ImageUrl = upload.BlobUri;
                    }
                }

                var success = await _storageService.UpdateProductAsync(id, vm);

                if (success)
                {
                    TempData["Success"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", "Failed to update product.");
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                ModelState.AddModelError("", "Error updating product. Please try again.");
                return View(vm);
            }
        }

        // GET: /Products/Delete/{id}
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest();

            var product = await _storageService.GetProductAsync(id);
            if (product == null)
                return NotFound();

            return View(product);
        }

        // POST: /Products/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest();

            try
            {
                var success = await _storageService.DeleteProductAsync(id);

                if (success)
                {
                    TempData["Success"] = "Product deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to delete product.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                TempData["Error"] = "Error deleting product.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
