using DonationApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DonationApp.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Item> Items { get; set; }
    public DbSet<ItemRequest> ItemRequests { get; set; }

    public DbSet<ItemImage> ItemImages { get; set; }

    public DbSet<ClaimRequest> ClaimRequests { get; set; }
    public DbSet<RequestOffer> RequestOffers { get; set; }

    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }

    public DbSet<Feedback> Feedbacks { get; set; }

    public DbSet<Notification> Notifications { get; set; }

    public DbSet<PointTransaction> PointTransactions { get; set; }

    public DbSet<Report> Reports { get; set; }
    public DbSet<AdminAction> AdminActions { get; set; }

    public DbSet<RequestLimit> RequestLimits { get; set; }

    public DbSet<RedeemItem> RedeemItems { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ItemImage>()
            .HasIndex(i => new { i.ItemId, i.OwnerType });

        builder.Entity<Feedback>()
            .HasIndex(f => new { f.ReviewerId, f.ClaimRequestId, f.ItemRequestId })
            .IsUnique();

        builder.Entity<Conversation>()
            .HasIndex(c => new { c.Type, c.RequesterId, c.DonorId });

        builder.Entity<PointTransaction>()
            .HasIndex(p => new { p.UserId, p.Type });

        builder.Entity<Report>()
            .HasOne(r => r.TargetUser)
            .WithMany()
            .HasForeignKey(r => r.TargetUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Report>()
            .HasOne(r => r.TargetRequest)
            .WithMany()
            .HasForeignKey(r => r.TargetRequestId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}