using System;

namespace ABCRetailers.Web.Models
{
    public class PaymentProofEntity
    {
        public string Id { get; set; } = string.Empty;

        // use this everywhere
        public string CustomerName { get; set; } = string.Empty;

        public string BlobName { get; set; } = string.Empty;
        public string BlobUri { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }
}
