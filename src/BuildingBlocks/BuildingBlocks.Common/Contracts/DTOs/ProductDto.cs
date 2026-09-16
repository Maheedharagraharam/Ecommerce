namespace BuildingBlocks.Common.Contracts.DTOs;

public record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    string? ImageUrl
);
