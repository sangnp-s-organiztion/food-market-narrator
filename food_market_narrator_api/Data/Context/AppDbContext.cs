
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using food_market_narrator_api.Models;  

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Bảng nhà hàng
    public DbSet<RestaurantModel> Restaurant { get; set; }
    public DbSet<RestaurantImageModel> RestaurantImage { get; set; }
    public DbSet<AudioModel> Audio { get; set; }
    public DbSet<LanguageModel> Language { get; set; }
}