using DonationApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DonationApp.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Listings
    public DbSet<Item> Items { get; set; }
    public DbSet<ItemRequest> ItemRequests { get; set; }

    // Unified image table (replaces ItemImages, RequestImages, RequestOfferImages)
    public DbSet<ItemImage> ItemImages { get; set; }

    // Claim & offer flow
    public DbSet<ClaimRequest> ClaimRequests { get; set; }
    public DbSet<RequestOffer> RequestOffers { get; set; }

    // Messaging
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }

    // Feedback (replaces UserReputations)
    public DbSet<Feedback> Feedbacks { get; set; }

    // Notifications
    public DbSet<Notification> Notifications { get; set; }

    // Points
    public DbSet<PointTransaction> PointTransactions { get; set; }

    // Moderation
    public DbSet<Report> Reports { get; set; }
    public DbSet<AdminAction> AdminActions { get; set; }

    // Limits
    public DbSet<RequestLimit> RequestLimits { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Enforce that ItemImage has exactly one FK set based on OwnerType
        builder.Entity<ItemImage>()
            .HasIndex(i => new { i.ItemId, i.OwnerType });

        // Enforce that Feedback has exactly one FK set
        builder.Entity<Feedback>()
            .HasIndex(f => new { f.ReviewerId, f.ClaimRequestId, f.RequestOfferId })
            .IsUnique();

        // Enforce that Conversation FK set matches its Type
        builder.Entity<Conversation>()
            .HasIndex(c => new { c.Type, c.RequesterId, c.DonorId });

        // PointTransaction — one FK per transaction
        builder.Entity<PointTransaction>()
            .HasIndex(p => new { p.UserId, p.Type });

        // Report — target is user OR donation, not both (enforced in app logic)
        builder.Entity<Report>()
            .HasOne(r => r.TargetUser)
            .WithMany()
            .HasForeignKey(r => r.TargetUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Report>()
            .HasOne(r => r.TargetDonation)
            .WithMany()
            .HasForeignKey(r => r.TargetDonationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
