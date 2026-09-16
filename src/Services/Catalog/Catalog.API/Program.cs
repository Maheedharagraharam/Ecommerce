using BuildingBlocks.Common.Services;
using Catalog.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Add SQLite EF Core DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=catalog.db";
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseSqlite(connectionString));

// 3. Register Azure Blob Storage Service (with local fallback)
builder.Services.AddScoped<IAzureBlobService, AzureBlobService>();

// 4. Add Health Checks
builder.Services.AddHealthChecks();

// 5. Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// Auto-migrate / Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAll");

// Enable serving files from local uploads folder if fallback is used
var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(uploadsDir))
{
    Directory.CreateDirectory(uploadsDir);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsDir),
    RequestPath = "/uploads"
});

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
