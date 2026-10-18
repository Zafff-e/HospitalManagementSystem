using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Hms.Application.Common.Interfaces;

namespace Hms.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;
    private readonly SymmetricSecurityKey _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessExpirationMinutes;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
        var secret = _config["JwtSettings:Secret"] ?? "ThisIsAVeryLongSecretKeyForDevelopmentOnly2024!SecureKeyForHms2026";
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        _issuer = _config["JwtSettings:Issuer"] ?? "hms-api";
        _audience = _config["JwtSettings:Audience"] ?? "hms-clients";
        _accessExpirationMinutes = int.TryParse(_config["JwtSettings:AccessTokenExpirationMinutes"], out var mins) ? mins : 15;
    }

    public (string Token, DateTime ExpiresAt) GenerateAccessToken(
        Guid userId, 
        string email, 
        string fullName, 
        Guid hospitalId, 
        IEnumerable<string> roles, 
        IEnumerable<string> permissions)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_accessExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Name, fullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("hospital_id", hospitalId.ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var perm in permissions)
        {
            claims.Add(new Claim("permission", perm));
        }

        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = creds
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);

        return (handler.WriteToken(token), expiresAt);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,
            ValidateLifetime = false // We need to read claims even if expired
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
