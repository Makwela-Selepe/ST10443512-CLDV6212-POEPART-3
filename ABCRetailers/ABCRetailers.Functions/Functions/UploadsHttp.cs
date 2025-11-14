using System.Net;
using ABCRetailers.Functions.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Net.Http.Headers;

namespace ABCRetailers.Functions.Functions
{
    public class UploadsHttp
    {
        private readonly UploadService _upload;

        public UploadsHttp(UploadService upload)
        {
            _upload = upload;
        }

        [Function("Uploads_ProofOfPayment")]
        public async Task<HttpResponseData> ProofOfPayment(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "uploads/proof-of-payment")]
            HttpRequestData req)
        {
            if (!req.Headers.TryGetValues("Content-Type", out var ctValues))
                return req.CreateResponse(HttpStatusCode.BadRequest);

            var contentType = ctValues.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(contentType) || !contentType.Contains("multipart/form-data"))
                return req.CreateResponse(HttpStatusCode.BadRequest);

            var mediaType = MediaTypeHeaderValue.Parse(contentType);
            var boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value;
            if (string.IsNullOrEmpty(boundary))
                return req.CreateResponse(HttpStatusCode.BadRequest);

            var reader = new MultipartReader(boundary, req.Body);
            MultipartSection? section;

            string? uploadedBlobName = null;
            Uri? uploadedBlobUri = null;

            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var cd) &&
                    cd.DispositionType.Equals("form-data") &&
                    !string.IsNullOrEmpty(cd.FileName.Value))
                {
                    var fileName = cd.FileName.Value!;
                    (string blobName, Uri blobUri) = await _upload.SaveProofAsync(section.Body, fileName);

                    // optional copy to file share
                    await _upload.CopyToFileShareAsync(blobName);

                    uploadedBlobName = blobName;
                    uploadedBlobUri = blobUri;
                    break;
                }
            }

            if (uploadedBlobName is null || uploadedBlobUri is null)
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("No file found in form-data.");
                return bad;
            }

            var ok = req.CreateResponse(HttpStatusCode.OK);
            await ok.WriteAsJsonAsync(new { ok = true, blobName = uploadedBlobName, blobUri = uploadedBlobUri });
            return ok;
        }
    }
}
