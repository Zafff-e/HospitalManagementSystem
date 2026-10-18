using System.Security.Claims;

namespace Hms.Application.Common.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(Guid userId, string email, string fullName, Guid hospitalId, IEnumerable<string> roles, IEnumerable<string> permissions);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
