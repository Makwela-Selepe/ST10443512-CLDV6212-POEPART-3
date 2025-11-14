using System.Linq;
using System.Threading.Tasks;
using ABCRetailers.Web.Models.ViewModels;
using ABCRetailers.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Web.Controllers
{
    [Authorize]
    public class UploadsController : Controller
    {
        private readonly AzureStorageService _storage;

        public UploadsController(AzureStorageService storage)
        {
            _storage = storage;
        }

        // CUSTOMER SIDE: upload proof
        [Authorize(Roles = "Customer")]
        [HttpGet]
        public IActionResult ProofOfPayment()
        {
            return View(new UploadProofOfPaymentViewModel());
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProofOfPayment(UploadProofOfPaymentViewModel model, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("File", "Please choose a file.");
                return View(model);
            }

            var upload = await _storage.UploadProofOfPaymentAsync(file);
            if (!upload.Ok)
            {
                ModelState.AddModelError("", "Upload failed. Please try again.");
                return View(model);
            }

            var customerName = User.Identity?.Name ?? "Unknown";

            // Save metadata in table
            await _storage.SavePaymentProofAsync(customerName, upload.BlobName, upload.BlobUri);

            // Fill the view-model
            model.UploadedOk = true;
            model.BlobUri = upload.BlobUri;   // ✅ use BlobUri, not FileUrl
            model.BlobName = upload.BlobName; // ✅ use BlobName, not FileName

            return View(model);
        }


        // ADMIN SIDE: list all payment proofs
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AdminList()
        {
            var entities = await _storage.GetPaymentProofsAsync();

            var model = entities.Select(e => new PaymentProofViewModel
            {
                Id = e.Id,
                CustomerName = e.CustomerName,
                BlobName = e.BlobName,
                BlobUri = e.BlobUri,
                UploadedAt = e.UploadedAt
            }).ToList();

            return View(model);
        }


    }
}
