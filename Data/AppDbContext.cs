using AuctionSystem.API.Models;
using Microsoft.EntityFrameworkCore;

namespace AuctionSystem.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Auction> Auctions => Set<Auction>();
    public DbSet<Bid> Bids => Set<Bid>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Username).HasMaxLength(50).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(200).IsRequired();
            entity.Property(u => u.Password).HasMaxLength(100).IsRequired();
            entity.Property(u => u.FirstName).HasMaxLength(100);
            entity.Property(u => u.LastName).HasMaxLength(100);
        });

        modelBuilder.Entity<Auction>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Name).HasMaxLength(200).IsRequired();
            entity.Property(a => a.Description).HasMaxLength(1000);
            entity.Property(a => a.Category).HasMaxLength(100);
            entity.Property(a => a.StartingPrice).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(a => a.CurrentPrice).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(a => a.StartDate).IsRequired();
            entity.Property(a => a.EndDate).IsRequired();
            entity.Property(a => a.Status).HasConversion<string>();
            entity.HasOne(a => a.Owner)
                  .WithMany()
                  .HasForeignKey(a => a.OwnerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Bid>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(b => b.PlacedAt).IsRequired();
            entity.HasOne(b => b.Auction)
                  .WithMany(a => a.Bids)
                  .HasForeignKey(b => b.AuctionId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(b => b.Bidder)
                  .WithMany()
                  .HasForeignKey(b => b.BidderId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
