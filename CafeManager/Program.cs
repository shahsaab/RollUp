using System.Text;
using CafeManager.API.Hubs;
using CafeManager.API.Middleware;
using CafeManager.Application.Services;
using CafeManager.Core.Entities;
using CafeManager.Core.Interfaces;
using CafeManager.Core.Services;
using CafeManager.Infrastructure.Authentication;
using CafeManager.Infrastructure.Persistence;
using CafeManager.Infrastructure.Repositories;
using CafeManager.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Multi-Tenancy Context ─────────────────────────────────────────────────────
builder.Services.AddScoped<ITenantContext, TenantContext>();

// ── Blazor Server ─────────────────────────────────────────────────────────────
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options => 
{
    options.DetailedErrors = true;
});

// ── Controllers + API Explorer ───────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── EF Core + Database Configuration ──────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var dbOption = builder.Configuration["DBOption"]?.Trim();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (string.Equals(dbOption, "Postgres", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("🗄️ Using PostgreSQL Database Provider.");
        options.UseNpgsql(connectionString, npgsqlOptions => 
            npgsqlOptions.EnableRetryOnFailure(3));
    }
    else if (string.Equals(dbOption, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("🗄️ Using SQLite Database Provider.");
        options.UseSqlite(string.IsNullOrWhiteSpace(connectionString) ? "Data Source=cafemanager.db" : connectionString);
    }
    else
    {
        // Default to MSSQL
        Console.WriteLine("🗄️ Using Microsoft SQL Server Database Provider.");
        options.UseSqlServer(connectionString, sqlOptions => 
            sqlOptions.EnableRetryOnFailure(3));
    }
});

// ── Generic Repository ────────────────────────────────────────────────────────
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// ── JWT Auth & Blazor Auth State ──────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtSecret))
        };

        // Allow JWT via SignalR query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/hubs")))
                    ctx.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddAuthorizationCore();

// ── Storage & Custom Auth State Provider ──────────────────────────────────────
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Infrastructure ────────────────────────────────────────────────────────────
builder.Services.AddScoped<JwtProvider>();

// ── Application Services ──────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<IQueueService, QueueService>();

// ── Menu & Order Services ─────────────────────────────────────────�// ── Database Migration ────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        if (context.Database.IsRelational())
        {
            context.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Database migration check: {ex.Message}");
    }
}

app.Run();ontactEmail = defaultTenant.ContactEmail };
            context.Vendors.Add(vendor);
            context.SaveChanges();

            defaultOutlet = new Outlet 
            { 
                Name = "Downtown Branch", 
                TenantId = defaultTenant.Id, 
                VendorId = vendor.Id, 
                IsActive = true,
                Address = defaultTenant.Address,
                Phone = defaultTenant.ContactPhone
            };
            context.Outlets.Add(defaultOutlet);
            context.SaveChanges();
        }

        // Seed Categories
        if (!context.Categories.IgnoreQueryFilters().Any())
        {
            var categories = new List<Category>
            {
                new() { Name = "Hot Coffee", SortOrder = 1, TenantId = defaultTenant.Id },
                new() { Name = "Cold Brews & Iced", SortOrder = 2, TenantId = defaultTenant.Id },
                new() { Name = "Bakery & Pastries", SortOrder = 3, TenantId = defaultTenant.Id },
                new() { Name = "Artisan Sandwiches", SortOrder = 4, TenantId = defaultTenant.Id },
                new() { Name = "Desserts", SortOrder = 5, TenantId = defaultTenant.Id }
            };
            context.Categories.AddRange(categories);
            context.SaveChanges();
        }

        // Seed Admin User
        if (!context.Users.IgnoreQueryFilters().Any())
        {
            var admin = new User
            {
                FullName     = "System Admin",
                Email        = "admin@scandish.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role         = CafeManager.Core.Enums.Role.Admin,
                TenantId     = defaultTenant.Id,
                OutletId     = defaultOutlet.Id,
                CreatedAt    = DateTime.UtcNow,
                IsActive     = true
            };
            context.Users.Add(admin);
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FATAL: Database initialization failed: {ex.Message}");
    }
}

app.Run();
