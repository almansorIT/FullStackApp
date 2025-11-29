using ClientApp.Models;

namespace ClientApp.Services
{
    public interface IProductApiService
    {
        /// <summary>
        /// Returns products or an error message; the consumer should check Success.
        /// </summary>
        Task<ServiceResponse<Product[]>> GetProductsAsync(CancellationToken cancellationToken = default);
    }
}
