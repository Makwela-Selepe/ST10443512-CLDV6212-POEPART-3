using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;

namespace ABCRetailers.Functions.Services
{
    public class QueueService
    {
        private readonly QueueServiceClient _svc;
        private readonly string _queueStockUpdates;

        public QueueService(IConfiguration cfg)
        {
            var conn = cfg["AzureWebJobsStorage"]
                       ?? throw new InvalidOperationException("Missing AzureWebJobsStorage connection string.");
            _svc = new QueueServiceClient(conn);
            _queueStockUpdates = cfg["QUEUE_STOCK_UPDATES"] ?? "stock-updates";
        }

        public async Task SendStockUpdateAsync(string message)
        {
            var q = _svc.GetQueueClient(_queueStockUpdates);
            await q.CreateIfNotExistsAsync();
            await q.SendMessageAsync(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(message)));
        }
    }
}
