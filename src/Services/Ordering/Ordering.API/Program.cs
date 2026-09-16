using BuildingBlocks.Common.Extensions;
using BuildingBlocks.Common.Resilience;
using Microsoft.EntityFrameworkCore;
using Ordering.API.Data;
using Ordering.API.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Add SQLite EF Core DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=ordering.db";
builder.Services.AddDbContext<OrderingDbContext>(options =>
    options.UseSqlite(connectionString));

// 3. Register Synchronous Inter-Service Communication (Typed HttpClient + Polly Resilience Policies)
var catalogUrl = builder.Configuration["Services:CatalogUrl"] ?? "http://localhost:5001";
builder.Services.AddHttpClient<ICatalogServiceClient, CatalogServiceClient>(client =>
{
    client.BaseAddress = new Uri(catalogUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
})
.AddPolicyHandler(PollyPolicies.GetRetryPolicy())
.AddPolicyHandler(PollyPolicies.GetCircuitBreakerPolicy());

// 4. Register Asynchronous Event-Driven Messaging (MassTransit with Azure Service Bus / In-Memory fallback)
builder.Services.AddAppMessaging(builder.Configuration);

// 5. Health Checks
builder.Services.AddHealthChecks();

// 6. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// Ensure Ordering database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ordering API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
