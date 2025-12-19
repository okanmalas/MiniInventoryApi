using Microsoft.EntityFrameworkCore;
using MiniInventoryApi.Models;

namespace MiniInventoryApi.Data;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Stok> Stoklar { get; set; }
    public DbSet<Kategori> Kategoriler { get; set; }
}