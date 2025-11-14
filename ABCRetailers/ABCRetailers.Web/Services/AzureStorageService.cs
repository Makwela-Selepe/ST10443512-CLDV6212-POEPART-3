using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ABCRetailers.Web.Models;
using ABCRetailers.Web.Models.ViewModels;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace ABCRetailers.Web.Services
{
    public class AzureStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly TableServiceClient _tableServiceClient;

        // Table names
        private const string ProductsTableName = "Products";
        private const string OrdersTableName = "Orders";
        private const string CustomersTableName = "Customers";

        // Partition keys
        private const string ProductsPartitionKey = "PRODUCTS";
        private const string OrdersPartitionKey = "ORDER";
        private const string CustomersPartitionKey = "CUSTOMER";

        // Blob containers
        private const string ProductImagesContainer = "product-images";
        private const string ProofOfPaymentsContainer = "proof-of-payments";

        private const string PaymentProofsTableName = "PaymentProofs";
        private const string PaymentProofsPartitionKey = "PAYMENTPROOF";

        public AzureStorageService(IConfiguration configuration)
        {
            var connectionString =
                configuration.GetConnectionString("AzureStorage") ??
                configuration["AzureStorage:ConnectionString"];

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Azure Storage connection string not configured.");

            _blobServiceClient = new BlobServiceClient(connectionString);
            _tableServiceClient = new TableServiceClient(connectionString);

            // Ensure tables & containers exist
            _tableServiceClient.CreateTableIfNotExists(ProductsTableName);
            _tableServiceClient.CreateTableIfNotExists(OrdersTableName);
            _tableServiceClient.CreateTableIfNotExists(CustomersTableName);
            _tableServiceClient.CreateTableIfNotExists(PaymentProofsTableName); // 👈 add this

            _blobServiceClient.GetBlobContainerClient(ProductImagesContainer)
                              .CreateIfNotExists(PublicAccessType.Blob);
            _blobServiceClient.GetBlobContainerClient(ProofOfPaymentsContainer)
                              .CreateIfNotExists(PublicAccessType.Blob);
        }

        private TableClient GetProductsTable() => _tableServiceClient.GetTableClient(ProductsTableName);
        private TableClient GetOrdersTable() => _tableServiceClient.GetTableClient(OrdersTableName);
        private TableClient GetCustomersTable() => _tableServiceClient.GetTableClient(CustomersTableName);
        private TableClient GetPaymentProofsTable() =>
    _tableServiceClient.GetTableClient(PaymentProofsTableName);

        // =====================================================================
        // PRODUCTS
        // =====================================================================

        private static ProductViewModel MapProduct(TableEntity e)
        {
            var vm = new ProductViewModel
            {
                RowKey = e.RowKey,
                Id = e.RowKey,
                ProductName = e.GetString("ProductName") ?? string.Empty,
                Description = e.GetString("Description"),
                Price = (decimal?)(e.GetDouble("Price")) ?? 0m,
                StockAvailable = e.GetInt32("StockAvailable")
                                 ?? (e.GetInt32("Quantity") ?? 0),
                ImageBlobName = e.GetString("ImageBlobName"),
                ImageUrl = e.GetString("ImageUrl")
            };

            if (e.TryGetValue("ProductId", out var productIdObj) && productIdObj != null)
            {
                vm.ProductId = Convert.ToInt32(productIdObj);
            }

            return vm;
        }

        public async Task<List<ProductViewModel>> GetProductsAsync()
        {
            var table = GetProductsTable();
            await table.CreateIfNotExistsAsync();

            var result = new List<ProductViewModel>();

            await foreach (var entity in table.QueryAsync<TableEntity>(e => e.PartitionKey == ProductsPartitionKey))
            {
                result.Add(MapProduct(entity));
            }

            return result;
        }

        public async Task<ProductViewModel?> GetProductAsync(string id)
        {
            var table = GetProductsTable();
            await table.CreateIfNotExistsAsync();

            try
            {
                var response = await table.GetEntityAsync<TableEntity>(ProductsPartitionKey, id);
                return MapProduct(response.Value);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }
        // ==================================================================
        // Compatibility helper for ShopController
        // Old name: GetAllProductsAsync → reuse GetProductsAsync
        // ==================================================================
        public Task<List<ProductViewModel>> GetAllProductsAsync()
        {
            return GetProductsAsync();
        }

        // alias for CartController / others
        public Task<ProductViewModel?> GetProductByIdAsync(string id) => GetProductAsync(id);

        public async Task<bool> CreateProductAsync(ProductViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Id))
                model.Id = Guid.NewGuid().ToString("N");

            model.RowKey ??= model.Id;

            var table = GetProductsTable();
            await table.CreateIfNotExistsAsync();

            var entity = new TableEntity(ProductsPartitionKey, model.RowKey)
            {
                ["ProductName"] = model.ProductName,
                ["Description"] = model.Description ?? string.Empty,
                ["Price"] = (double)model.Price,
                ["StockAvailable"] = model.StockAvailable,
                ["ProductId"] = model.ProductId ?? 0,
                ["ImageBlobName"] = model.ImageBlobName ?? string.Empty,
                ["ImageUrl"] = model.ImageUrl ?? string.Empty
            };

            await table.UpsertEntityAsync(entity, TableUpdateMode.Replace);
            return true;
        }

        public async Task<bool> UpdateProductAsync(string id, ProductViewModel model)
        {
            model.Id = id;
            model.RowKey = id;
            return await CreateProductAsync(model);
        }

        public async Task<bool> DeleteProductAsync(string id)
        {
            var table = GetProductsTable();
            await table.CreateIfNotExistsAsync();

            try
            {
                await table.DeleteEntityAsync(ProductsPartitionKey, id);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return false;
            }
        }

        // Used by AdminController which builds a ProductEntity
        public async Task AddOrUpdateProductAsync(ProductEntity product)
        {
            var table = GetProductsTable();
            await table.CreateIfNotExistsAsync();

            if (string.IsNullOrWhiteSpace(product.Id))
                product.Id = Guid.NewGuid().ToString("N");

            var entity = new TableEntity(ProductsPartitionKey, product.Id)
            {
                ["ProductName"] = product.ProductName,
                ["Description"] = product.Description ?? string.Empty,
                ["Price"] = (double)product.Price,
                ["Quantity"] = product.Quantity,
                ["ImageBlobName"] = product.ImageBlobName ?? string.Empty,
                ["ImageUrl"] = product.ImageUrl ?? string.Empty
            };

            await table.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        public async Task<BlobUploadResultModel> UploadProductImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return new BlobUploadResultModel { Ok = false };

            var container = _blobServiceClient.GetBlobContainerClient(ProductImagesContainer);
            await container.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var ext = Path.GetExtension(file.FileName);
            var blobName = $"{Guid.NewGuid():N}{ext}";
            var blob = container.GetBlobClient(blobName);

            using (var stream = file.OpenReadStream())
            {
                await blob.UploadAsync(stream, overwrite: true);
            }

            return new BlobUploadResultModel
            {
                Ok = true,
                BlobName = blobName,
                BlobUri = blob.Uri.ToString()
            };
        }

        // =====================================================================
        // CUSTOMERS
        // =====================================================================

        private static CustomerEntity MapCustomerFromEntity(TableEntity entity)
        {
            return new CustomerEntity
            {
                Id = entity.RowKey,
                Name = entity.GetString("Name") ?? string.Empty,
                Email = entity.GetString("Email") ?? string.Empty,
                Phone = entity.GetString("Phone") ?? string.Empty,
                DeliveryAddress = entity.GetString("DeliveryAddress") ?? string.Empty
            };
        }

        public async Task<List<CustomerEntity>> GetCustomersAsync()
        {
            var table = GetCustomersTable();
            await table.CreateIfNotExistsAsync();

            var result = new List<CustomerEntity>();

            await foreach (var entity in table.QueryAsync<TableEntity>(e => e.PartitionKey == CustomersPartitionKey))
            {
                result.Add(MapCustomerFromEntity(entity));
            }

            return result;
        }

        public async Task<CustomerEntity?> GetCustomerByIdAsync(string id)
        {
            var table = GetCustomersTable();
            await table.CreateIfNotExistsAsync();

            try
            {
                var response = await table.GetEntityAsync<TableEntity>(CustomersPartitionKey, id);
                return MapCustomerFromEntity(response.Value);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task AddOrUpdateCustomerAsync(CustomerEntity customer)
        {
            var table = GetCustomersTable();
            await table.CreateIfNotExistsAsync();

            if (string.IsNullOrWhiteSpace(customer.Id))
                customer.Id = Guid.NewGuid().ToString("N");

            var entity = new TableEntity(CustomersPartitionKey, customer.Id)
            {
                ["Name"] = customer.Name,
                ["Email"] = customer.Email,
                ["Phone"] = customer.Phone,
                ["DeliveryAddress"] = customer.DeliveryAddress
            };

            await table.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }
        public async Task SavePaymentProofAsync(string customerName, string blobName, string blobUri)
        {
            var table = GetPaymentProofsTable();
            await table.CreateIfNotExistsAsync();

            var id = Guid.NewGuid().ToString("N");

            var entity = new TableEntity(PaymentProofsPartitionKey, id)
            {
                ["CustomerName"] = customerName,
                ["BlobName"] = blobName,
                ["BlobUri"] = blobUri,
                ["UploadedAt"] = DateTime.UtcNow
            };

            await table.UpsertEntityAsync(entity);
        }

        public async Task<List<PaymentProofEntity>> GetPaymentProofsAsync()
        {
            var table = GetPaymentProofsTable();
            await table.CreateIfNotExistsAsync();

            var list = new List<PaymentProofEntity>();

            await foreach (var e in table.QueryAsync<TableEntity>(x => x.PartitionKey == PaymentProofsPartitionKey))
            {
                list.Add(new PaymentProofEntity
                {
                    Id = e.RowKey,
                    CustomerName = e.GetString("CustomerName") ?? "",
                    BlobName = e.GetString("BlobName") ?? "",
                    BlobUri = e.GetString("BlobUri") ?? "",
                    UploadedAt = e.GetDateTime("UploadedAt") ?? DateTime.UtcNow
                });
            }

            return list;
        }


        public async Task<bool> DeleteCustomerAsync(string id)
        {
            var table = GetCustomersTable();
            await table.CreateIfNotExistsAsync();

            try
            {
                await table.DeleteEntityAsync(CustomersPartitionKey, id);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return false;
            }
        }

        // =====================================================================
        // ORDERS
        // =====================================================================

        private static TableEntity MapOrderToEntity(OrderEntity order)
        {
            return new TableEntity(OrdersPartitionKey, order.Id)
            {
                ["CustomerId"] = order.CustomerId,
                ["CustomerName"] = order.CustomerName,
                ["ProductId"] = order.ProductId,
                ["ProductName"] = order.ProductName,
                ["Quantity"] = order.Quantity,
                ["TotalAmount"] = (double)order.TotalAmount,
                ["Status"] = order.Status,
                ["CreatedAt"] = order.CreatedAt,
                ["ShippedAt"] = order.ShippedAt
            };
        }

        private static OrderEntity MapOrderFromEntity(TableEntity entity)
        {
            return new OrderEntity
            {
                Id = entity.RowKey,
                CustomerId = entity.GetString("CustomerId") ?? string.Empty,
                CustomerName = entity.GetString("CustomerName") ?? string.Empty,
                ProductId = entity.GetString("ProductId") ?? string.Empty,
                ProductName = entity.GetString("ProductName") ?? string.Empty,
                Quantity = entity.GetInt32("Quantity") ?? 0,
                TotalAmount = (decimal?)(entity.GetDouble("TotalAmount")) ?? 0m,
                Status = entity.GetString("Status") ?? "Pending",
                CreatedAt = entity.GetDateTime("CreatedAt") ?? DateTime.UtcNow,
                ShippedAt = entity.GetDateTime("ShippedAt")
            };
        }

        public async Task<List<OrderEntity>> GetOrdersAsync()
        {
            var table = GetOrdersTable();
            await table.CreateIfNotExistsAsync();

            var result = new List<OrderEntity>();

            await foreach (var entity in table.QueryAsync<TableEntity>(e => e.PartitionKey == OrdersPartitionKey))
            {
                result.Add(MapOrderFromEntity(entity));
            }

            return result;
        }

        public async Task<OrderEntity?> GetOrderByIdAsync(string id)
        {
            var table = GetOrdersTable();
            await table.CreateIfNotExistsAsync();

            try
            {
                var response = await table.GetEntityAsync<TableEntity>(OrdersPartitionKey, id);
                return MapOrderFromEntity(response.Value);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<OrderEntity> CreateOrderAsync(OrderEntity order)
        {
            var table = GetOrdersTable();
            await table.CreateIfNotExistsAsync();

            if (string.IsNullOrWhiteSpace(order.Id))
                order.Id = Guid.NewGuid().ToString("N");

            order.CreatedAt = DateTime.UtcNow;

            var entity = MapOrderToEntity(order);
            await table.AddEntityAsync(entity);

            return order;
        }

        /// <summary>
        /// Delete only if Status == "Shipped".
        /// </summary>
        public async Task<bool> DeleteOrderIfShippedAsync(string orderId)
        {
            var table = GetOrdersTable();
            await table.CreateIfNotExistsAsync();

            try
            {
                var response = await table.GetEntityAsync<TableEntity>(OrdersPartitionKey, orderId);
                var entity = response.Value;

                var status = entity.GetString("Status") ?? "Pending";
                if (!string.Equals(status, "Shipped", StringComparison.OrdinalIgnoreCase))
                    return false;

                await table.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return false;
            }
       
        }
        // ==================================================================
        // Compatibility helpers for OrdersController (partitionKey/rowKey)
        // ==================================================================

        // Old name used by OrdersController → simply reuse GetOrdersAsync
        public Task<List<OrderEntity>> GetAllOrdersAsync()
        {
            return GetOrdersAsync();
        }

        // Old signature with partitionKey/rowKey → we only need the rowKey (Id)
        public Task<OrderEntity?> GetOrderAsync(string partitionKey, string rowKey)
        {
            // partitionKey is ignored because we always use "ORDER" internally
            return GetOrderByIdAsync(rowKey);
        }

        // Old delete signature → unconditional delete, controller checks "Shipped"
        public async Task DeleteOrderAsync(string partitionKey, string rowKey)
        {
            var table = GetOrdersTable();

            // Our orders are stored with PartitionKey = "ORDER"
            await table.DeleteEntityAsync("ORDER", rowKey);
        }
        public async Task<bool> UpdateOrderStatusAsync(string orderId, string status)
        {
            var table = GetOrdersTable();
            await table.CreateIfNotExistsAsync();

            try
            {
                var response = await table.GetEntityAsync<TableEntity>(OrdersPartitionKey, orderId);
                var entity = response.Value;

                entity["Status"] = status;

                // If marking as shipped, also set ShippedAt
                if (string.Equals(status, "Shipped", StringComparison.OrdinalIgnoreCase))
                    entity["ShippedAt"] = DateTime.UtcNow;

                await table.UpsertEntityAsync(entity, TableUpdateMode.Replace);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return false;
            }
        }

        // =====================================================================
        // PROOF OF PAYMENT UPLOAD
        // =====================================================================

        public async Task<BlobUploadResultModel> UploadProofOfPaymentAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return new BlobUploadResultModel { Ok = false };

            var container = _blobServiceClient.GetBlobContainerClient(ProofOfPaymentsContainer);
            await container.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var ext = Path.GetExtension(file.FileName);
            var blobName = $"{Guid.NewGuid():N}{ext}";
            var blob = container.GetBlobClient(blobName);

            using (var stream = file.OpenReadStream())
            {
                await blob.UploadAsync(stream, overwrite: true);
            }

            return new BlobUploadResultModel
            {
                Ok = true,
                BlobName = blobName,
                BlobUri = blob.Uri.ToString()
            };
        }
    }

    // Simple result DTO used for both product images and proof-of-payment
    public class BlobUploadResultModel
    {
        public bool Ok { get; set; }
        public string BlobName { get; set; } = string.Empty;
        public string BlobUri { get; set; } = string.Empty;
    }
}
