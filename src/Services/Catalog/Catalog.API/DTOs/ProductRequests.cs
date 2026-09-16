using System.ComponentModel.DataAnnotations;

namespace Catalog.API.DTOs;

public record CreateProductRequest(
    [Required] string Name,
    string Description,
    [Range(0.01, 100000)] decimal Price,
    [Range(0, 100000)] int StockQuantity,
    string? ImageUrl
);

public record UpdateProductRequest(
    [Required] string Name,
    string Description,
    [Range(0.01, 100000)] decimal Price,
    [Range(0, 100000)] int StockQuantity,
    string? ImageUrl
);
