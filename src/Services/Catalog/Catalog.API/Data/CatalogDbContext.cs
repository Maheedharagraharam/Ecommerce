using Catalog.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Data;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed initial realistic products for immediate testing
        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Wireless Noise-Canceling Headphones",
                Description = "Premium over-ear Bluetooth headphones with active noise cancellation and 30hr battery life.",
                Price = 199.99m,
                StockQuantity = 25,
                ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=500"
            },
            new Product
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Mechanical Gaming Keyboard",
                Description = "Tactile mechanical switches with per-key RGB backlighting and durable aluminum frame.",
                Price = 89.50m,
                StockQuantity = 40,
                ImageUrl = "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=500"
            },
            new Product
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Ultra-HD 4K Monitor 27-inch",
                Description = "IPS display with 144Hz refresh rate, 1ms response time, and HDR400 color precision.",
                Price = 349.00m,
                StockQuantity = 15,
                ImageUrl = "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=500"
            }
        );
    }
}
