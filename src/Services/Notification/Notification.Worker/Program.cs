using BuildingBlocks.Common.Extensions;
using BuildingBlocks.Common.Services;
using MassTransit;
using Notification.Worker.Consumers;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Register Notification Store & Azure Blob Service
builder.Services.AddSingleton<INotificationStore, InMemoryNotificationStore>();
builder.Services.AddScoped<IAzureBlobService, AzureBlobService>();

// 3. Register MassTransit Message Bus with OrderCreatedConsumer
builder.Services.AddAppMessaging(builder.Configuration, bus =>
{
    bus.AddConsumer<OrderCreatedConsumer>();
});

// 4. Add Health Checks and CORS
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification Worker v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
