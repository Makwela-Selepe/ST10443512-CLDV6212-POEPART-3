using System.Net;
using System.Text.Json;
using ABCRetailers.Functions.Models;
using ABCRetailers.Functions.Services;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace ABCRetailers.Functions.Functions
{
    public class OrdersHttp
    {
        private readonly TableService _tables;

        public OrdersHttp(TableService tables)
        {
            _tables = tables;
        }

        [Function("Orders_List")]
        public async Task<HttpResponseData> List(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders")]
            HttpRequestData req)
        {
            try
            {
                var client = _tables.Orders();
                var items = client.Query<OrderEntity>(x => x.PartitionKey == "ORDER").ToArray();
                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync(items);
                return ok;
            }
            catch (Exception ex)
            {
                var err = req.CreateResponse(HttpStatusCode.InternalServerError);
                await err.WriteStringAsync(ex.ToString());
                return err;
            }
        }

        [Function("Orders_Get")]
        public async Task<HttpResponseData> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/{id}")]
            HttpRequestData req,
            string id)
        {
            try
            {
                var t = _tables.Orders();
                var e = (await t.GetEntityAsync<OrderEntity>("ORDER", id)).Value;
                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync(e);
                return ok;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }
        }

        [Function("Orders_Create")]
        public async Task<HttpResponseData> Create(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")]
            HttpRequestData req)
        {
            try
            {
                var dto = await JsonSerializer.DeserializeAsync<OrderEntity>(
                    req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (dto is null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Body required.");
                    return bad;
                }

                dto.PartitionKey = "ORDER";
                if (string.IsNullOrWhiteSpace(dto.RowKey))
                    dto.RowKey = Guid.NewGuid().ToString("N");

                if (dto.CreatedAt == default)
                    dto.CreatedAt = DateTime.UtcNow;

                var client = _tables.Orders();
                await client.AddEntityAsync(dto);

                var ok = req.CreateResponse(HttpStatusCode.Created);
                await ok.WriteAsJsonAsync(dto);
                return ok;
            }
            catch (Exception ex)
            {
                var err = req.CreateResponse(HttpStatusCode.InternalServerError);
                await err.WriteStringAsync(ex.ToString());
                return err;
            }
        }

        [Function("Orders_UpdateStatus")]
        public async Task<HttpResponseData> UpdateStatus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "orders/{id}/status")]
            HttpRequestData req,
            string id)
        {
            try
            {
                var dto = await JsonSerializer.DeserializeAsync<OrderEntity>(
                    req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (dto is null || string.IsNullOrWhiteSpace(dto.Status))
                    return req.CreateResponse(HttpStatusCode.BadRequest);

                var client = _tables.Orders();

                var current = await client.GetEntityAsync<OrderEntity>("ORDER", id);
                var entity = current.Value;
                entity.Status = dto.Status;

                if (string.Equals(dto.Status, "Shipped", StringComparison.OrdinalIgnoreCase))
                    entity.ShippedAt = DateTime.UtcNow;

                await client.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteAsJsonAsync(entity);
                return ok;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }
        }

        [Function("Orders_Delete")]
        public async Task<HttpResponseData> Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "orders/{id}")]
            HttpRequestData req,
            string id)
        {
            var client = _tables.Orders();
            await client.DeleteEntityAsync("ORDER", id, ETag.All);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}
