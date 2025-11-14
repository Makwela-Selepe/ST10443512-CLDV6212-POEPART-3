using System.Net;
using System.Text.Json;
using ABCRetailers.Functions.Models;
using ABCRetailers.Functions.Services;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Net.Http.Headers;

namespace ABCRetailers.Functions.Functions
{
    public class ProductsHttp
    {
        private readonly TableService _tables;
        private readonly QueueService _queues;
        private readonly CounterService _counters;
        private readonly UploadService _uploads;

        public ProductsHttp(TableService tables, QueueService queues, CounterService counters, UploadService uploads)
        {
            _tables = tables;
            _queues = queues;
            _counters = counters;
            _uploads = uploads;
        }

        [Function("Products_List")]
        public async Task<HttpResponseData> List(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products")] HttpRequestData req)
        {
            var client = _tables.Products();
            var items = client.Query<ProductEntity>(x => x.PartitionKey == "PRODUCTS").ToList();

            foreach (var p in items)
            {
                if (!string.IsNullOrWhiteSpace(p.ImageBlobName))
                    p.ImageUrl = _uploads.GetProductImageSas(p.ImageBlobName)?.ToString();
            }

            var ok = req.CreateResponse(HttpStatusCode.OK);
            await ok.WriteAsJsonAsync(items);
            return ok;
        }

        [Function("Products_Get")]
        public async Task<HttpResponseData> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products/{id}")] HttpRequestData req,
            string id)
        {
            try
            {
                var t = _tables.Products();
                var e = (await t.GetEntityAsync<ProductEntity>("PRODUCTS", id)).Value;

                if (!string.IsNullOrWhiteSpace(e.ImageBlobName))
                    e.ImageUrl = _uploads.GetProductImageSas(e.ImageBlobName)?.ToString();

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync(e);
                return ok;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }
        }

        [Function("Products_Create")]
        public async Task<HttpResponseData> Create(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "products")] HttpRequestData req)
        {
            var dto = await JsonSerializer.DeserializeAsync<ProductEntity>(
                req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dto is null)
                return req.CreateResponse(HttpStatusCode.BadRequest);

            dto.PartitionKey = "PRODUCTS";
            dto.RowKey = Guid.NewGuid().ToString("N");
            dto.ProductId = await _counters.NextAsync("PRODUCT_ID");

            var client = _tables.Products();
            await client.AddEntityAsync(dto);

            await _queues.SendStockUpdateAsync($"PRODUCT:{dto.ProductId}:{dto.StockAvailable}");

            var ok = req.CreateResponse(HttpStatusCode.Created);
            await ok.WriteAsJsonAsync(dto);
            return ok;
        }

        [Function("Products_Update")]
        public async Task<HttpResponseData> Update(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "products/{id}")] HttpRequestData req,
            string id)
        {
            var dto = await JsonSerializer.DeserializeAsync<ProductEntity>(
                req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dto is null) return req.CreateResponse(HttpStatusCode.BadRequest);

            dto.PartitionKey = "PRODUCTS";
            dto.RowKey = id;

            var t = _tables.Products();
            await t.UpsertEntityAsync(dto, TableUpdateMode.Replace);

            var ok = req.CreateResponse(HttpStatusCode.OK);
            await ok.WriteAsJsonAsync(dto);
            return ok;
        }

        [Function("Products_UploadImage")]
        public async Task<HttpResponseData> UploadImage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "products/{id}/image")]
            HttpRequestData req,
            string id)
        {
            if (!req.Headers.TryGetValues("Content-Type", out var vals))
                return req.CreateResponse(HttpStatusCode.BadRequest);

            var contentType = vals.FirstOrDefault();
            if (contentType is null || !contentType.Contains("multipart/form-data"))
                return req.CreateResponse(HttpStatusCode.BadRequest);

            var mediaType = MediaTypeHeaderValue.Parse(contentType);
            var boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value;
            if (string.IsNullOrEmpty(boundary))
                return req.CreateResponse(HttpStatusCode.BadRequest);

            var reader = new MultipartReader(boundary, req.Body);
            MultipartSection? section;

            string? blobName = null;
            string? ct = null;

            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var cd) &&
                    cd.DispositionType.Equals("form-data") &&
                    !string.IsNullOrEmpty(cd.FileName.Value))
                {
                    var fileName = cd.FileName.Value!;
                    ct = section.ContentType;
                    blobName = await _uploads.SaveProductImageAsync(section.Body, fileName, ct);
                    break;
                }
            }

            if (blobName is null)
                return req.CreateResponse(HttpStatusCode.BadRequest);

            var t = _tables.Products();
            var entity = (await t.GetEntityAsync<ProductEntity>("PRODUCTS", id)).Value;
            entity.ImageBlobName = blobName;
            await t.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);

            var url = _uploads.GetProductImageSas(blobName)?.ToString();
            var ok = req.CreateResponse(HttpStatusCode.OK);
            await ok.WriteAsJsonAsync(new { imageBlobName = blobName, imageUrl = url });
            return ok;
        }
    }
}
