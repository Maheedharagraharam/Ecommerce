using Microsoft.AspNetCore.Mvc;
using Notification.Worker.Consumers;

namespace Notification.Worker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationStore _store;
    private readonly BuildingBlocks.Common.Services.IAzureBlobService _blobService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationStore store,
        BuildingBlocks.Common.Services.IAzureBlobService blobService,
        ILogger<NotificationsController> logger)
    {
        _store = store;
        _blobService = blobService;
        _logger = logger;
    }

    /// <summary>
    /// View the log of all processed order notifications and sent emails
    /// </summary>
    [HttpGet]
    public ActionResult<IEnumerable<NotificationRecord>> GetAll()
    {
        return Ok(_store.GetAll());
    }

    /// <summary>
    /// Process and dispatch an order notification
    /// </summary>
    [HttpPost("dispatch")]
    public async Task<IActionResult> Dispatch([FromBody] BuildingBlocks.Common.Contracts.Events.OrderCreatedEvent message)
    {
        _logger.LogInformation("================================================================");
        _logger.LogInformation("📨 [NOTIFICATION SERVICE] Processing order notification for Order ID: {OrderId}", message.OrderId);
        _logger.LogInformation("Customer: {CustomerName} ({CustomerEmail})", message.CustomerName, message.CustomerEmail);
        _logger.LogInformation("Total Amount: {TotalAmount:C}", message.TotalAmount);

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

        return Ok(notificationRecord);
    }
}
