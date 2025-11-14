public class PaymentProofViewModel
{
    public string Id { get; set; } = default!;
    public string CustomerName { get; set; } = default!;
    public string BlobName { get; set; } = default!;
    public string BlobUri { get; set; } = default!;
    public DateTime UploadedAt { get; set; }
}
