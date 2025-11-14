using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ABCRetailers.Web.Models.ViewModels
{
    public class UploadProofOfPaymentViewModel
    {
        [Required]
        [Display(Name = "File")]
        public IFormFile? File { get; set; }

        // Indicates upload success
        public bool UploadedOk { get; set; }

        // Where in blob storage the file lives
        public string? BlobUri { get; set; }

        // Blob/file name
        public string? BlobName { get; set; }
    }
}
