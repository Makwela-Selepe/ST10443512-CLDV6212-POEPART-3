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
    public class CustomersHttp
    {
        private readonly TableService _tables;

        public CustomersHttp(TableService tables)
        {
            _tables = tables;
        }

        [Function("Customers_List")]
        public async Task<HttpResponseData> List(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers")]
            HttpRequestData req)
        {
            var client = _tables.Customers();
            var items = client.Query<CustomerEntity>(x => x.PartitionKey == "CUSTOMER").ToArray();

            var res = req.CreateResponse(HttpStatusCode.OK);
            await res.WriteAsJsonAsync(items);
            return res;
        }

        [Function("Customers_Get")]
        public async Task<HttpResponseData> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers/{id}")]
            HttpRequestData req,
            string id)
        {
            var client = _tables.Customers();
            try
            {
                var entity = await client.GetEntityAsync<CustomerEntity>("CUSTOMER", id);
                var res = req.CreateResponse(HttpStatusCode.OK);
                await res.WriteAsJsonAsync(entity.Value);
                return res;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }
        }

        [Function("Customers_Create")]
        public async Task<HttpResponseData> Create(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers")]
            HttpRequestData req)
        {
            var client = _tables.Customers();

            var dto = await JsonSerializer.DeserializeAsync<CustomerEntity>(
                req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new CustomerEntity();

            dto.PartitionKey = "CUSTOMER";
            if (string.IsNullOrWhiteSpace(dto.RowKey))
                dto.RowKey = Guid.NewGuid().ToString("N");

            await client.AddEntityAsync(dto);

            var res = req.CreateResponse(HttpStatusCode.Created);
            await res.WriteAsJsonAsync(dto);
            return res;
        }

        [Function("Customers_Update")]
        public async Task<HttpResponseData> Update(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "customers/{id}")]
            HttpRequestData req,
            string id)
        {
            var client = _tables.Customers();

            var dto = await JsonSerializer.DeserializeAsync<CustomerEntity>(
                req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dto is null)
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Body required.");
                return bad;
            }

            dto.PartitionKey = "CUSTOMER";
            dto.RowKey = id;

            await client.UpsertEntityAsync(dto, TableUpdateMode.Replace);

            var ok = req.CreateResponse(HttpStatusCode.OK);
            await ok.WriteAsJsonAsync(dto);
            return ok;
        }
    }
}
