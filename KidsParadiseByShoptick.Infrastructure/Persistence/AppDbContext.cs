using KidsParadiseByShoptick.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KidsParadiseByShoptick.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ToyCategory> ToyCategories => Set<ToyCategory>();
    public DbSet<Toy> Toys => Set<Toy>();
    public DbSet<ToyImage> ToyImages => Set<ToyImage>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<SiteImage> SiteImages => Set<SiteImage>();
    public DbSet<AffiliatePartner> AffiliatePartners => Set<AffiliatePartner>();
    public DbSet<AffiliateLedgerEntry> AffiliateLedgerEntries => Set<AffiliateLedgerEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(e =>
        {
            e.HasIndex(x => x.Whatsapp).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Whatsapp).HasMaxLength(50);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.Address).HasMaxLength(500);
        });

        modelBuilder.Entity<ToyCategory>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.ImagePath).HasMaxLength(500);
        });

        modelBuilder.Entity<Toy>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(300);
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.Property(x => x.SalePrice).HasPrecision(18, 2);
            e.Property(x => x.VideoLink).HasMaxLength(500);
            e.Property(x => x.VideoFilePath).HasMaxLength(500);
            e.HasOne(x => x.Category).WithMany(x => x.Toys).HasForeignKey(x => x.CategoryId);
        });

        modelBuilder.Entity<ToyImage>(e =>
        {
            e.Property(x => x.ImagePath).HasMaxLength(500);
            e.HasIndex(x => x.PerceptualHash);
            e.HasOne(x => x.Toy).WithMany(x => x.Images).HasForeignKey(x => x.ToyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.HasIndex(x => x.OrderNumber).IsUnique();
            e.Property(x => x.OrderNumber).HasMaxLength(50);
            e.Property(x => x.SubTotal).HasPrecision(18, 2);
            e.Property(x => x.DeliveryCharge).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.Property(x => x.AdvanceAmount).HasPrecision(18, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Whatsapp).HasMaxLength(50);
            e.Property(x => x.TrackingNumber).HasMaxLength(100);
            e.HasOne(x => x.Customer).WithMany(x => x.Orders).HasForeignKey(x => x.CustomerId);
            e.HasOne(x => x.AffiliatePartner).WithMany(x => x.Orders).HasForeignKey(x => x.AffiliatePartnerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AffiliatePartner>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Whatsapp).HasMaxLength(50);
            e.Property(x => x.AccountNumber).HasMaxLength(100);
            e.Property(x => x.WalletBankName).HasMaxLength(120);
            e.Property(x => x.Code).HasMaxLength(50);
        });

        modelBuilder.Entity<AffiliateLedgerEntry>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.OrderNumber).HasMaxLength(50);
            e.HasIndex(x => new { x.OrderId, x.Type });
            e.HasOne(x => x.AffiliatePartner).WithMany(x => x.LedgerEntries)
                .HasForeignKey(x => x.AffiliatePartnerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId);
            e.HasOne(x => x.Toy).WithMany(x => x.OrderItems).HasForeignKey(x => x.ToyId);
        });

        modelBuilder.Entity<Review>(e =>
        {
            e.Property(x => x.ReviewerName).HasMaxLength(200);
            e.Property(x => x.Comment).HasMaxLength(2000);
            e.Property(x => x.ImagePath).HasMaxLength(500);
            e.HasIndex(x => new { x.OrderId, x.ToyId }).IsUnique();
            e.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Toy).WithMany(x => x.Reviews).HasForeignKey(x => x.ToyId);
            e.HasOne(x => x.Customer).WithMany(x => x.Reviews).HasForeignKey(x => x.CustomerId);
        });

        modelBuilder.Entity<AdminUser>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Username).HasMaxLength(100);
            e.Property(x => x.Password).HasMaxLength(200);
        });

        modelBuilder.Entity<SiteImage>(e =>
        {
            e.HasIndex(x => x.Key).IsUnique();
            e.Property(x => x.Key).HasMaxLength(100);
            e.Property(x => x.Label).HasMaxLength(200);
            e.Property(x => x.Group).HasMaxLength(100);
            e.Property(x => x.ImagePath).HasMaxLength(500);
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Subtitle).HasMaxLength(500);
            e.Property(x => x.CtaText).HasMaxLength(100);
            e.Property(x => x.LinkUrl).HasMaxLength(300);
            e.Property(x => x.TitleColor).HasMaxLength(20);
            e.Property(x => x.SubtitleColor).HasMaxLength(20);
            e.Property(x => x.CtaColor).HasMaxLength(20);
        });
    }
}
