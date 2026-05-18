using DonationApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DonationApp.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Item> Items { get; set; }
    public DbSet<ItemImage> ItemImages { get; set; }
    public DbSet<ClaimRequest> ClaimRequests { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<ItemRequest> ItemRequests { get; set; }
    public DbSet<RequestImage> RequestImages { get; set; }
    public DbSet<RequestOffer> RequestOffers { get; set; }
    public DbSet<RequestOfferImage> RequestOfferImages { get; set; }
    public DbSet<UserReputation> UserReputations { get; set; }
}