using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;

namespace ABCRetailers.Functions.Services
{
    public class TableService
    {
        private readonly TableServiceClient _svc;

        public TableService(IConfiguration cfg)
        {
            var conn = cfg["AzureWebJobsStorage"]
                       ?? throw new InvalidOperationException("Missing AzureWebJobsStorage connection string.");
            _svc = new TableServiceClient(conn);
        }

        public TableClient Products()
        {
            // must be "Products" to match Web AzureStorageService
            var c = _svc.GetTableClient("Products");
            c.CreateIfNotExists();
            return c;
        }

        public TableClient Customers()
        {
            var c = _svc.GetTableClient("Customers");
            c.CreateIfNotExists();
            return c;
        }

        public TableClient Orders()
        {
            var c = _svc.GetTableClient("Orders");
            c.CreateIfNotExists();
            return c;
        }
    }
}
