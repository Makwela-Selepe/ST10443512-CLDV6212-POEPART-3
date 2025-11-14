using ABCRetailers.Web.Models.ViewModels;
using Microsoft.AspNetCore.Http;

namespace ABCRetailers.Web.Services
{
    public class FunctionsApi
    {
        private readonly HttpClient _httpClient;

        public FunctionsApi(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Product methods
        public async Task<List<ProductViewModel>> GetProductsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("products");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<ProductViewModel>>() ?? new List<ProductViewModel>();
                }
            }
            catch (Exception)
            {
                // Return empty list if API is not available
            }
            return new List<ProductViewModel>();
        }

        public async Task<ProductViewModel?> GetProductAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"products/{id}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ProductViewModel>();
                }
            }
            catch (Exception)
            {
                // Return null if API is not available
            }
            return null;
        }

        public async Task<bool> CreateProductAsync(ProductViewModel product)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("products", product);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateProductAsync(string id, ProductViewModel product)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"products/{id}", product);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(string id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"products/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UploadProductImageAsync(string id, IFormFile image)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                using var stream = image.OpenReadStream();
                content.Add(new StreamContent(stream), "file", image.FileName);

                var response = await _httpClient.PostAsync($"products/{id}/upload", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Customer methods
        public async Task<List<CustomerViewModel>> GetCustomersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("customers");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<CustomerViewModel>>() ?? new List<CustomerViewModel>();
                }
            }
            catch (Exception)
            {
                // Return empty list if API is not available
            }
            return new List<CustomerViewModel>();
        }

        // Order methods
        public async Task<List<OrderViewModel>> GetOrdersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("orders");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<OrderViewModel>>() ?? new List<OrderViewModel>();
                }
            }
            catch (Exception)
            {
                // Return empty list if API is not available
            }
            return new List<OrderViewModel>();
        }

        public async Task<bool> CreateOrderAsync(OrderViewModel order)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("orders", order);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(string id, string status)
        {
            try
            {
                var response = await _httpClient.PatchAsync($"orders/{id}/status?status={status}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Upload methods
        public async Task<(bool Ok, string BlobName, string BlobUri)> UploadProofOfPaymentAsync(IFormFile file)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                using var stream = file.OpenReadStream();
                content.Add(new StreamContent(stream), "file", file.FileName);

                var response = await _httpClient.PostAsync("uploads/proof", content);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<UploadResult>();
                    return (true, result?.BlobName ?? "", result?.BlobUri ?? "");
                }
            }
            catch (Exception)
            {
                // Upload failed
            }
            return (false, "", "");
        }

        private class UploadResult
        {
            public string BlobName { get; set; } = "";
            public string BlobUri { get; set; } = "";
        }
    }
}