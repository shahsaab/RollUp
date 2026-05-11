using CafeManager.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeManager.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── DbSets ────────────────────────────────────────────────────────────────
    public DbSet<User> Users => Set<User>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Outlet> Outlets => Set<Outlet>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Core.Entities.MenuItem> MenuItems => Set<Core.Entities.MenuItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<QueueEntry> QueueEntries => Set<QueueEntry>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Global soft-delete query filters ──────────────────────────────────
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Vendor>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Outlet>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Category>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Core.Entities.MenuItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Order>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<OrderItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<QueueEntry>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Payment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SubscriptionPlan>().HasQueryFilter(e => !e.IsDeleted);

        // ── User ──────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasConversion<string>();
        });

        // ── Vendor → Outlets (1:many) ─────────────────────────────────────────
        modelBuilder.Entity<Outlet>(e =>
        {
            e.HasOne(o => o.Vendor)
             .WithMany(v => v.Outlets)
             .HasForeignKey(o => o.VendorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Outlet → MenuItems (1:many) ───────────────────────────────────────
        modelBuilder.Entity<Core.Entities.MenuItem>(e =>
        {
            e.HasOne(m => m.Outlet)
             .WithMany(o => o.MenuItems)
             .HasForeignKey(m => m.OutletId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(m => m.Category)
             .WithMany(c => c.MenuItems)
             .HasForeignKey(m => m.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);

            e.Property(m => m.Price).HasPrecision(18, 2);
        });

        // ── Order ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Order>(e =>
        {
            e.HasOne(o => o.Outlet)
             .WithMany(out_ => out_.Orders)
             .HasForeignKey(o => o.OutletId)
             .OnDelete(DeleteBehavior.Restrict);

            e.Property(o => o.Status).HasConversion<string>();
            e.Property(o => o.Type).HasConversion<string>();
        });

        // ── OrderItem ─────────────────────────────────────────────────────────
        modelBuilder.Entity<OrderItem>(e =>
        {
            e.HasOne(oi => oi.Order)
             .WithMany(o => o.Items)
             .HasForeignKey(oi => oi.OrderId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(oi => oi.MenuItem)
             .WithMany(m => m.OrderItems)
             .HasForeignKey(oi => oi.MenuItemId)
             .OnDelete(DeleteBehavior.Restrict);

            e.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
            e.Ignore(oi => oi.LineTotal); // computed, not persisted
        });

        // ── Payment (1:1 with Order) ──────────────────────────────────────────
        modelBuilder.Entity<Payment>(e =>
        {
            e.HasOne(p => p.Order)
             .WithOne(o => o.Payment)
             .HasForeignKey<Payment>(p => p.OrderId)
             .OnDelete(DeleteBehavior.Cascade);

            e.Property(p => p.Amount).HasPrecision(18, 2);
            e.Property(p => p.Status).HasConversion<string>();
        });

        // ── QueueEntry ────────────────────────────────────────────────────────
        modelBuilder.Entity<QueueEntry>(e =>
        {
            e.HasOne(q => q.Outlet)
             .WithMany(o => o.QueueEntries)
             .HasForeignKey(q => q.OutletId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── SubscriptionPlan ──────────────────────────────────────────────────
        modelBuilder.Entity<SubscriptionPlan>(e =>
        {
            e.HasOne(s => s.Vendor)
             .WithMany(v => v.SubscriptionPlans)
             .HasForeignKey(s => s.VendorId)
             .OnDelete(DeleteBehavior.Cascade);

            e.Property(s => s.MonthlyPrice).HasPrecision(18, 2);
        });
    }

    /// <summary>Automatically set UpdatedAt on every SaveChanges call.</summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Core.Entities.BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
