using System.Net.Http.Json;
using BuildingBlocks.Common.Contracts.DTOs;

namespace Ordering.API.Services;

public interface ICatalogServiceClient
{
    Task<ProductDto?> GetProductByIdAsync(Guid productId);
}

public class CatalogServiceClient : ICatalogServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogServiceClient> _logger;

    public CatalogServiceClient(HttpClient httpClient, ILogger<CatalogServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid productId)
    {
        try
        {
            _logger.LogInformation("Calling Catalog Service for Product ID: {ProductId}", productId);
            var response = await _httpClient.GetAsync($"/api/products/{productId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Catalog Service returned status code {StatusCode} for Product ID {ProductId}", response.StatusCode, productId);
                return null;
            }

            var product = await response.Content.ReadFromJsonAsync<ProductDto>();
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during HTTP communication with Catalog Service for Product ID {ProductId}", productId);
            throw;
        }
    }
}
