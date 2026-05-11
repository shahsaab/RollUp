namespace CafeManager.Core.Entities;

/// <summary>
/// EF Core-mapped MenuItem entity for persistence.
/// The lightweight <see cref="CafeManager.Core.Models.MenuItem"/> model remains in use by mock services.
/// </summary>
public class MenuItem : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public bool IsPopular { get; set; } = false;

    // Stored as comma-separated; can be normalised later
    public string Tags { get; set; } = string.Empty;

    // Navigation
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public int OutletId { get; set; }
    public Outlet Outlet { get; set; } = null!;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
