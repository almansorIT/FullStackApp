using System.Net.Http.Json;
using ClientApp.Models;
using Microsoft.Extensions.Logging;

namespace ClientApp.Services
{
    public class ServiceResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public int? StatusCode { get; set; }
    }

    public class ProductApiService : IProductApiService
    {
        private readonly HttpClient _http;
        private readonly ILogger<ProductApiService>? _logger;

        // Timeout for product requests (client-side guard)
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

        public ProductApiService(HttpClient http, ILogger<ProductApiService>? logger = null)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<ServiceResponse<Product[]>> GetProductsAsync(CancellationToken cancellationToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(RequestTimeout);

            try
            {
                // Use a GET and check status code explicitly so we can return a friendly error message
                var response = await _http.GetAsync("/api/products", cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cts.Token);
                    var msg = $"API responded {(int)response.StatusCode} {response.ReasonPhrase}: {content}";
                    _logger?.LogWarning("GetProducts returned non-success: {Msg}", msg);
                    return new ServiceResponse<Product[]>
                    {
                        Success = false,
                        ErrorMessage = msg,
                        StatusCode = (int)response.StatusCode
                    };
                }

                Product[]? products;
                try
                {
                    products = await response.Content.ReadFromJsonAsync<Product[]?>(cancellationToken: cts.Token);
                }
                catch (System.Text.Json.JsonException jsonEx)
                {
                    // Failed to deserialize JSON — do not log the exception details to the browser console.
                    _logger?.LogWarning("Failed to parse products JSON response (malformed JSON)");
                    return new ServiceResponse<Product[]>
                    {
                        Success = false,
                        ErrorMessage = "Received malformed data from server. Please try again later."
                    };
                }

                if (products == null)
                {
                    var msg = "API returned no product data.";
                    _logger?.LogWarning(msg);
                    return new ServiceResponse<Product[]>
                    {
                        Success = false,
                        ErrorMessage = msg
                    };
                }

                return new ServiceResponse<Product[]>
                {
                    Success = true,
                    Data = products
                };
            }
            catch (OperationCanceledException)
            {
                var msg = "Request timed out or was cancelled.";
                _logger?.LogWarning(msg);
                return new ServiceResponse<Product[]>
                {
                    Success = false,
                    ErrorMessage = msg
                };
            }
            catch (HttpRequestException httpEx)
            {
                // Network / connectivity issues
                _logger?.LogWarning(httpEx, "Network error while calling products API");
                return new ServiceResponse<Product[]>
                {
                    Success = false,
                    ErrorMessage = "Unable to contact the product service. Please check your connection and try again."
                };
            }
            catch (Exception ex)
            {
                // Generic fallback — log details, return safe message to the UI.
                _logger?.LogError(ex, "Unexpected error fetching products");
                return new ServiceResponse<Product[]>
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred while loading products."
                };
            }
        }
    }
}
