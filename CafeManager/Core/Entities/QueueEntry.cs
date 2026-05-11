namespace CafeManager.Core.Entities;

public class QueueEntry : BaseEntity
{
    public string CustomerName { get; set; } = string.Empty;
    public int PartySize { get; set; } = 1;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsSeated { get; set; } = false;
    public DateTime? SeatedAt { get; set; }
    public int Position { get; set; }

    // Navigation
    public int OutletId { get; set; }
    public Outlet Outlet { get; set; } = null!;
}
