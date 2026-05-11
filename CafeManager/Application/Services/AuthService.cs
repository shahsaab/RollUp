using CafeManager.Application.DTOs;
using CafeManager.Core.Entities;
using CafeManager.Core.Enums;
using CafeManager.Core.Interfaces;
using CafeManager.Infrastructure.Authentication;

namespace CafeManager.Application.Services;

public class AuthService : IAuthService
{
    private readonly IRepository<User> _users;
    private readonly JwtProvider _jwt;

    public AuthService(IRepository<User> users, JwtProvider jwt)
    {
        _users = users;
        _jwt   = jwt;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var user = await _users.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);
        if (user is null) return null;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        var (token, expiresAt) = _jwt.Generate(user);
        return BuildResponse(user, token, expiresAt);
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto request)
    {
        var exists = await _users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (exists is not null) return null; // email taken

        var role = Enum.TryParse<Role>(request.Role, true, out var r) ? r : Role.Cashier;

        var user = new User
        {
            FullName     = request.FullName,
            Email        = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role         = role,
            OutletId     = request.OutletId
        };

        await _users.AddAsync(user);
        await _users.SaveChangesAsync();

        var (token, expiresAt) = _jwt.Generate(user);
        return BuildResponse(user, token, expiresAt);
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user is null) return false;

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        _users.Update(user);
        await _users.SaveChangesAsync();
        return true;
    }

    private static AuthResponseDto BuildResponse(User user, string token, DateTime expiresAt) => new()
    {
        Token     = token,
        ExpiresAt = expiresAt,
        User = new UserDto
        {
            Id       = user.Id,
            FullName = user.FullName,
            Email    = user.Email,
            Role     = user.Role.ToString(),
            OutletId = user.OutletId
        }
    };
}
