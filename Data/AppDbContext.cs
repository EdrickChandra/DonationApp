using Microsoft.EntityFrameworkCore;
using DonationApp.Models;

namespace DonationApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Item> Items { get; set; }
}
