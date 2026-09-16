using System.ComponentModel.DataAnnotations;

namespace Ordering.API.DTOs;

public record OrderItemRequest(
    [Required] Guid ProductId,
    [Range(1, 100)] int Quantity
);

public record CreateOrderRequest(
    [Required, EmailAddress] string CustomerEmail,
    [Required] string CustomerName,
    [Required, MinLength(1)] List<OrderItemRequest> Items
);

public record OrderResponse(
    Guid Id,
    string CustomerEmail,
    string CustomerName,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt,
    List<OrderItemResponse> Items
);

public record OrderItemResponse(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity
);
