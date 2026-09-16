using BuildingBlocks.Common.Contracts.Events;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ordering.API.Data;
using Ordering.API.DTOs;
using Ordering.API.Models;
using Ordering.API.Services;

namespace Ordering.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderingDbContext _context;
    private readonly ICatalogServiceClient _catalogService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        OrderingDbContext context,
        ICatalogServiceClient catalogService,
        IPublishEndpoint publishEndpoint,
        IConfiguration configuration,
        ILogger<OrdersController> logger)
    {
        _context = context;
        _catalogService = catalogService;
        _publishEndpoint = publishEndpoint;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Get all orders
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetAll()
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var response = orders.Select(MapToResponse).ToList();
        return Ok(response);
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound(new { message = $"Order with ID {id} not found." });
        }

        return Ok(MapToResponse(order));
    }

    /// <summary>
    /// Place a new order (demonstrates synchronous HTTP validation + asynchronous event publishing)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create([FromBody] CreateOrderRequest request)
    {
        _logger.LogInformation("Processing new order placement for customer: {CustomerEmail}", request.CustomerEmail);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerEmail = request.CustomerEmail,
            CustomerName = request.CustomerName,
            Status = OrderStatus.Confirmed,
            CreatedAt = DateTime.UtcNow
        };

        decimal totalAmount = 0;
        var eventItems = new List<OrderItemDto>();

        // 1. Synchronous Communication: Validate each item with Catalog Microservice over HTTP
        foreach (var item in request.Items)
        {
            var product = await _catalogService.GetProductByIdAsync(item.ProductId);
            if (product == null)
            {
                return BadRequest(new { message = $"Product with ID {item.ProductId} does not exist in Catalog Service." });
            }

            if (product.StockQuantity < item.Quantity)
            {
                return BadRequest(new
                {
                    message = $"Insufficient stock for product '{product.Name}'. Available: {product.StockQuantity}, Requested: {item.Quantity}."
                });
            }

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = item.Quantity
            };

            order.Items.Add(orderItem);
            totalAmount += orderItem.UnitPrice * orderItem.Quantity;

            eventItems.Add(new OrderItemDto(
                product.Id,
                product.Name,
                orderItem.UnitPrice,
                orderItem.Quantity
            ));
        }

        order.TotalAmount = totalAmount;

        // 2. Persist Order in Ordering Microservice Database
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} successfully saved to database with total {TotalAmount:C}", order.Id, order.TotalAmount);

        // 3. Asynchronous Communication: Publish OrderCreatedEvent via MassTransit (Azure Service Bus / In-Memory)
        var orderCreatedEvent = new OrderCreatedEvent(
            order.Id,
            order.CustomerEmail,
            order.CustomerName,
            order.TotalAmount,
            order.CreatedAt,
            eventItems
        );

        await _publishEndpoint.Publish(orderCreatedEvent);
        _logger.LogInformation("Published OrderCreatedEvent to Message Bus for Order ID: {OrderId}", order.Id);

        // Notify Notification Worker in local multi-process development
        var notificationUrl = _configuration["Services:NotificationUrl"] ?? "http://localhost:5003";
        _ = Task.Run(async () =>
        {
            try
            {
                using var httpClient = new HttpClient();
                await httpClient.PostAsJsonAsync($"{notificationUrl}/api/notifications/dispatch", orderCreatedEvent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not send direct notification dispatch to {Url}", notificationUrl);
            }
        });

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, MapToResponse(order));
    }

    private static OrderResponse MapToResponse(Order order) => new(
        order.Id,
        order.CustomerEmail,
        order.CustomerName,
        order.TotalAmount,
        order.Status.ToString(),
        order.CreatedAt,
        order.Items.Select(i => new OrderItemResponse(
            i.ProductId,
            i.ProductName,
            i.UnitPrice,
            i.Quantity
        )).ToList()
    );
}
