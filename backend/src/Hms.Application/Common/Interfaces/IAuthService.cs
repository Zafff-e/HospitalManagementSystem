using Hms.Application.Auth.Dtos;

namespace Hms.Application.Common.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress = null, CancellationToken cancellationToken = default);
    Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress = null, CancellationToken cancellationToken = default);
    Task<bool> RevokeTokenAsync(string refreshToken, string? ipAddress = null, CancellationToken cancellationToken = default);
    Task<AuthUserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
