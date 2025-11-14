using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;

namespace ABCRetailers.Functions.Services
{
    public class CounterService
    {
        private readonly TableClient _counters;

        private class CounterEntity : ITableEntity
        {
            public string PartitionKey { get; set; } = "COUNTER";
            public string RowKey { get; set; } = default!;
            public long Value { get; set; }
            public DateTimeOffset? Timestamp { get; set; }
            public ETag ETag { get; set; }
        }

        public CounterService(IConfiguration cfg)
        {
            var conn = cfg["AzureWebJobsStorage"]
                       ?? throw new InvalidOperationException("Missing AzureWebJobsStorage connection string.");
            var svc = new TableServiceClient(conn);
            _counters = svc.GetTableClient("Counters");
            _counters.CreateIfNotExists();
        }

        public async Task<int> NextAsync(string name)
        {
            while (true)
            {
                CounterEntity entity;
                try
                {
                    entity = (await _counters.GetEntityAsync<CounterEntity>("COUNTER", name)).Value;
                }
                catch
                {
                    entity = new CounterEntity { RowKey = name, Value = 0, ETag = ETag.All };
                    await _counters.UpsertEntityAsync(entity);
                }

                var next = entity.Value + 1;
                entity.Value = next;
                try
                {
                    await _counters.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);
                    return (int)next;
                }
                catch
                {
                    await Task.Delay(10);
                }
            }
        }
    }
}
