using CafeManager.Application.DTOs;

namespace CafeManager.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginRequestDto request);
    Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto request);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
}
