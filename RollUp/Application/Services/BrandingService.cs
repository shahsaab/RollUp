using Microsoft.EntityFrameworkCore;
using RollUp.Application.DTOs;
using RollUp.Core.Interfaces;
using RollUp.Infrastructure.Persistence;

namespace RollUp.Application.Services;

public class BrandingService : IBrandingService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public BrandingService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<TenantBrandingDto> GetBrandingAsync()
    {
        var tenantId = _tenantContext.CurrentTenantId ?? 1;
        var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId)
                     ?? await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync();

        if (tenant == null)
        {
            return new TenantBrandingDto
            {
                BusinessName = "RollUp Cafe",
                Tagline = "Artisan Cafe & Bakery",
                ThemeTemplate = "bistro",
                ColorScheme = "espresso",
                FontFamily = "inter",
                Currency = "USD"
            };
        }

        return new TenantBrandingDto
        {
            BusinessName = tenant.Name,
            Tagline = tenant.Tagline ?? "Artisan Cafe & Quick Orders",
            LogoUrl = tenant.LogoUrl,
            Address = tenant.Address,
            ContactPhone = tenant.ContactPhone,
            ContactEmail = tenant.ContactEmail,
            Currency = tenant.Currency ?? "USD",
            ThemeTemplate = string.IsNullOrWhiteSpace(tenant.ThemeTemplate) ? "bistro" : tenant.ThemeTemplate,
            ColorScheme = string.IsNullOrWhiteSpace(tenant.ColorScheme) ? "espresso" : tenant.ColorScheme,
            FontFamily = string.IsNullOrWhiteSpace(tenant.FontFamily) ? "inter" : tenant.FontFamily,
            CustomPrimaryColor = tenant.CustomPrimaryColor,
            CustomAccentColor = tenant.CustomAccentColor
        };
    }

    public async Task<bool> UpdateBrandingAsync(TenantBrandingDto dto)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? 1;
        var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId)
                     ?? await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync();

        if (tenant == null) return false;

        tenant.Name = dto.BusinessName.Trim();
        tenant.Tagline = dto.Tagline?.Trim();
        tenant.LogoUrl = dto.LogoUrl;
        tenant.Address = dto.Address.Trim();
        tenant.ContactPhone = dto.ContactPhone.Trim();
        tenant.ContactEmail = dto.ContactEmail.Trim();
        tenant.Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "USD" : dto.Currency.ToUpper();
        tenant.ThemeTemplate = dto.ThemeTemplate;
        tenant.ColorScheme = dto.ColorScheme;
        tenant.FontFamily = dto.FontFamily;
        tenant.CustomPrimaryColor = dto.CustomPrimaryColor;
        tenant.CustomAccentColor = dto.CustomAccentColor;
        tenant.UpdatedAt = DateTime.UtcNow;

        _db.Tenants.Update(tenant);
        await _db.SaveChangesAsync();
        return true;
    }
}
