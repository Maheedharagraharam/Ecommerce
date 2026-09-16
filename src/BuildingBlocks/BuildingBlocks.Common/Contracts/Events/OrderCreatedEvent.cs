namespace BuildingBlocks.Common.Contracts.Events;

public record OrderItemDto(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity
);

public record OrderCreatedEvent(
    Guid OrderId,
    string CustomerEmail,
    string CustomerName,
    decimal TotalAmount,
    DateTime CreatedAt,
    List<OrderItemDto> Items
);
