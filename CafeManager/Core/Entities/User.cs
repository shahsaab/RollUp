using CafeManager.Core.Enums;

namespace CafeManager.Core.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; } = Role.Cashier;
    public bool IsActive { get; set; } = true;

    // Navigation
    public int? OutletId { get; set; }
    public Outlet? Outlet { get; set; }
}
