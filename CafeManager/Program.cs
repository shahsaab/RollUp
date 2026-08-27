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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

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
    else
    {
        // Default to MSSQL (if DBOption is "MSSQL", missing, or unrecognized)
        Console.WriteLine("🗄️ Using Microsoft SQL Server Database Provider.");
        options.UseSqlServer(connectionString, sqlOptions => 
            sqlOptions.EnableRetryOnFailure(3));
    }
});

// ── Generic Repository ────────────────────────────────────────────────────────
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// ── JWT Auth ──────────────────────────────────────────────────────────────────
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

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Infrastructure ────────────────────────────────────────────────────────────
builder.Services.AddScoped<JwtProvider>();

// ── Application Services ──────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<IQueueService, QueueService>();

// ── Mock Services (transitioning to DB-backed Scoped services) ───────────
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddSingleton<IOrderNotificationService, OrderNotificationService>();

// ═════════════════════════════════════════════════════════════════════════════
var app = builder.Build();

// ── Global Exception Handler ──────────────────────────────────────────────────
// app.UseMiddleware<ExceptionMiddleware>();

// ── HTTP Pipeline ─────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();

// ── Auth ──────────────────────────────────────────────────────────────────────
app.UseAuthentication();
app.UseAuthorization();

// ── REST API endpoints ────────────────────────────────────────────────────────
app.MapControllers();

// ── SignalR Hubs ──────────────────────────────────────────────────────────────
app.MapHub<QueueHub>("/hubs/queue");
app.MapHub<OrderHub>("/hubs/orders");

// ── Blazor ────────────────────────────────────────────────────────────────────
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// ── Data Seeding ──────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    try
    {
        context.Database.EnsureCreated();
        
        // Seed Vendor & Outlet
        if (!context.Vendors.Any())
        {
            var vendor = new Vendor { Name = "Scandish Cafe Corp", ContactEmail = "contact@scandish.com" };
            context.Vendors.Add(vendor);
            context.SaveChanges();

            var outlet = new Outlet { Name = "Main Branch", VendorId = vendor.Id, IsActive = true };
            context.Outlets.Add(outlet);
            context.SaveChanges();
        }

        // Seed Categories
        if (!context.Categories.Any())
        {
            var categories = new List<Category>
            {
                new Category { Name = "Hot Drinks", SortOrder = 1 },
                new Category { Name = "Cold Drinks", SortOrder = 2 },
                new Category { Name = "Pastries", SortOrder = 3 },
                new Category { Name = "Sandwiches", SortOrder = 4 },
                new Category { Name = "Desserts", SortOrder = 5 }
            };
            context.Categories.AddRange(categories);
            context.SaveChanges();
        }

        // Seed Admin User
        if (!context.Users.Any())
        {
            var admin = new User
            {
                FullName = "System Admin",
                Email = "admin@scandish.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = CafeManager.Core.Enums.Role.Admin,
                CreatedAt = DateTime.UtcNow
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
