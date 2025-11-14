using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Files.Shares;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;

namespace ABCRetailers.Functions.Services
{
    public class UploadService
    {
        private readonly string _conn;
        private readonly BlobServiceClient _blob;
        private readonly ShareServiceClient _share;
        private readonly string _imagesContainer;
        private readonly string _proofsContainer;
        private readonly string _contractsShare;

        public UploadService(IConfiguration cfg)
        {
            _conn = cfg["AzureWebJobsStorage"]
                    ?? throw new InvalidOperationException("Missing AzureWebJobsStorage connection string.");

            _blob = new BlobServiceClient(_conn);
            _share = new ShareServiceClient(_conn);

            _imagesContainer = cfg["BLOB_PRODUCT_IMAGES"] ?? "product-images";
            _proofsContainer = cfg["BLOB_PAYMENT_PROOFS"] ?? "proof-of-payments";   // match web side
            _contractsShare = cfg["FILESHARE_CONTRACTS"] ?? "contracts";
        }

        private BlobContainerClient Images() => _blob.GetBlobContainerClient(_imagesContainer);
        private BlobContainerClient Proofs() => _blob.GetBlobContainerClient(_proofsContainer);

        public async Task<string> SaveProductImageAsync(Stream content, string fileName, string? contentType)
        {
            var c = Images();
            await c.CreateIfNotExistsAsync();
            var blobName = $"{Guid.NewGuid():N}-{fileName}";
            var blob = c.GetBlobClient(blobName);
            await blob.UploadAsync(content, overwrite: true);

            if (!string.IsNullOrWhiteSpace(contentType))
            {
                await blob.SetHttpHeadersAsync(new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = contentType });
            }

            return blobName;
        }

        public async Task<(string blobName, Uri blobUri)> SaveProofAsync(Stream content, string fileName)
        {
            var c = Proofs();
            await c.CreateIfNotExistsAsync();
            var blobName = $"{Guid.NewGuid():N}-{fileName}";
            var blob = c.GetBlobClient(blobName);
            await blob.UploadAsync(content, overwrite: true);
            return (blobName, blob.Uri);
        }

        public async Task CopyToFileShareAsync(string sourceBlobName)
        {
            var c = Images();
            await c.CreateIfNotExistsAsync();
            var blob = c.GetBlobClient(sourceBlobName);

            var share = _share.GetShareClient(_contractsShare);
            await share.CreateIfNotExistsAsync();
            var dir = share.GetRootDirectoryClient();
            await dir.CreateIfNotExistsAsync();
            var file = dir.GetFileClient(sourceBlobName);

            var sas = GetProductImageSas(sourceBlobName, 10);
            if (sas is null) return;

            await file.StartCopyAsync(sas);
        }

        public Uri? GetProductImageSas(string blobName, int minutes = 30)
        {
            var c = Images();
            var blob = c.GetBlobClient(blobName);

            if (blob.CanGenerateSasUri)
            {
                var b = new BlobSasBuilder(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(minutes))
                {
                    BlobContainerName = c.Name,
                    BlobName = blobName,
                    Resource = "b"
                };
                return blob.GenerateSasUri(b);
            }

            var (account, key) = ParseNameKey(_conn) ?? default;
            if (account is null || key is null) return blob.Uri;

            var cred = new StorageSharedKeyCredential(account, key);
            var b2 = new BlobSasBuilder
            {
                BlobContainerName = c.Name,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-1),
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(minutes)
            };
            b2.SetPermissions(BlobSasPermissions.Read);
            var ub = new BlobUriBuilder(blob.Uri) { Sas = b2.ToSasQueryParameters(cred) };
            return ub.ToUri();
        }

        private static (string account, string key)? ParseNameKey(string conn)
        {
            string? name = null, key = null;
            foreach (var p in conn.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                if (p.StartsWith("AccountName=", StringComparison.OrdinalIgnoreCase))
                    name = p["AccountName=".Length..];
                if (p.StartsWith("AccountKey=", StringComparison.OrdinalIgnoreCase))
                    key = p["AccountKey=".Length..];
            }
            return (name is not null && key is not null) ? (name, key) : null;
        }
    }
}
