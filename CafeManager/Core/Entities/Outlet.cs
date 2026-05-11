namespace CafeManager.Core.Entities;

public class Outlet : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation
    public int VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    public ICollection<User> Staff { get; set; } = new List<User>();
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<QueueEntry> QueueEntries { get; set; } = new List<QueueEntry>();
}
