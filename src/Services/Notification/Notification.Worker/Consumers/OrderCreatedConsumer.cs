using BuildingBlocks.Common.Contracts.Events;
using BuildingBlocks.Common.Services;
using MassTransit;

namespace Notification.Worker.Consumers;

public record NotificationRecord(
    Guid OrderId,
    string CustomerEmail,
    string CustomerName,
    decimal TotalAmount,
    DateTime SentAt,
    string Message
);

public interface INotificationStore
{
    void Add(NotificationRecord record);
    IReadOnlyList<NotificationRecord> GetAll();
}

public class InMemoryNotificationStore : INotificationStore
{
    private readonly List<NotificationRecord> _records = new();
    private readonly object _lock = new();

    public void Add(NotificationRecord record)
    {
        lock (_lock)
        {
            _records.Insert(0, record);
        }
    }

    public IReadOnlyList<NotificationRecord> GetAll()
    {
        lock (_lock)
        {
            return _records.ToList();
        }
    }
}

public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly INotificationStore _store;
    private readonly IAzureBlobService _blobService;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(
        INotificationStore store,
        IAzureBlobService blobService,
        ILogger<OrderCreatedConsumer> logger)
    {
        _store = store;
        _blobService = blobService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation("================================================================");
        _logger.LogInformation("📨 [NOTIFICATION SERVICE] Received OrderCreatedEvent for Order ID: {OrderId}", message.OrderId);
        _logger.LogInformation("Customer: {CustomerName} ({CustomerEmail})", message.CustomerName, message.CustomerEmail);
        _logger.LogInformation("Total Amount: {TotalAmount:C}", message.TotalAmount);
        _logger.LogInformation("Items Count: {ItemCount}", message.Items.Count);

        foreach (var item in message.Items)
        {
            _logger.LogInformation("  -> {Quantity}x {ProductName} @ {UnitPrice:C}", item.Quantity, item.ProductName, item.UnitPrice);
        }

        // Simulate generating an invoice text file and uploading to Azure Blob Storage
        var invoiceContent = $"INVOICE FOR ORDER {message.OrderId}\nCustomer: {message.CustomerName} ({message.CustomerEmail})\nDate: {message.CreatedAt:u}\nTotal: {message.TotalAmount:C}\n";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(invoiceContent));
        var invoiceFileName = $"invoice_{message.OrderId}.txt";
        var invoiceUrl = await _blobService.UploadFileAsync(stream, invoiceFileName, "text/plain");

        _logger.LogInformation("📄 Invoice saved to Azure Blob Storage / Storage fallback: {InvoiceUrl}", invoiceUrl);
        _logger.LogInformation("✉️ Simulated Email Confirmation sent successfully to: {CustomerEmail}", message.CustomerEmail);
        _logger.LogInformation("================================================================");

        var notificationRecord = new NotificationRecord(
            message.OrderId,
            message.CustomerEmail,
            message.CustomerName,
            message.TotalAmount,
            DateTime.UtcNow,
            $"Order #{message.OrderId} confirmed. Invoice generated at: {invoiceUrl}"
        );

        _store.Add(notificationRecord);
    }
}
