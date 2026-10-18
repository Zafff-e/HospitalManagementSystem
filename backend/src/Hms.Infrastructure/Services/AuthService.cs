using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Hms.Application.Auth.Dtos;
using Hms.Application.Common.Interfaces;
using Hms.Domain.Exceptions;
using Hms.Infrastructure.Identity;
using Hms.Infrastructure.Persistence;

namespace Hms.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtService;
    private readonly HmsDbContext _db;
    private readonly IAuditService _auditService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtService,
        HmsDbContext db,
        IAuditService auditService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _db = db;
        _auditService = auditService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            throw new DomainException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new DomainException("Account is inactive. Please contact your administrator.");
        }

        // Check if locked out before attempting sign in
        if (await _userManager.IsLockedOutAsync(user))
        {
            throw new DomainException("Account is locked out due to multiple failed login attempts. Please try again in 15 minutes.");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            await _auditService.LogAsync("AccountLocked", "ApplicationUser", user.Id.ToString(), cancellationToken: cancellationToken);
            throw new DomainException("Account is locked out due to 5 consecutive failed login attempts. Please try again in 15 minutes.");
        }

        if (!result.Succeeded)
        {
            throw new DomainException("Invalid email or password.");
        }

        // Reset access failed count on successful login
        await _userManager.ResetAccessFailedCountAsync(user);

        // Fetch user roles
        var roles = await _userManager.GetRolesAsync(user);

        // Fetch permissions mapped to these roles
        var roleEntities = await _db.Roles
            .Where(r => roles.Contains(r.Name!))
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .ToListAsync(cancellationToken);

        var permissions = roleEntities
            .SelectMany(r => r.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToList();

        // Generate tokens
        var (accessToken, expiresAt) = _jwtService.GenerateAccessToken(
            user.Id, 
            user.Email ?? string.Empty, 
            user.FullName, 
            user.HospitalId, 
            roles, 
            permissions);

        var refreshTokenString = _jwtService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = ipAddress
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Login", "ApplicationUser", user.Id.ToString(), cancellationToken: cancellationToken);

        var userDto = new AuthUserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            user.StaffId,
            user.BmdcRegNo,
            user.Designation,
            user.HospitalId,
            roles,
            permissions
        );

        return new LoginResponse(accessToken, refreshTokenString, expiresAt, userDto);
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
        {
            throw new DomainException("Invalid access token or token structure.");
        }

        var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new DomainException("Invalid user identifier in token.");
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || !user.IsActive)
        {
            throw new DomainException("User does not exist or is inactive.");
        }

        var existingRefreshToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken && r.UserId == userId, cancellationToken);

        if (existingRefreshToken == null || !existingRefreshToken.IsActive)
        {
            throw new DomainException("Refresh token is invalid, expired, or has already been used.");
        }

        // Generate rotating refresh token
        var newRefreshTokenString = _jwtService.GenerateRefreshToken();

        existingRefreshToken.RevokedAt = DateTime.UtcNow;
        existingRefreshToken.RevokedByIp = ipAddress;
        existingRefreshToken.ReplacedByToken = newRefreshTokenString;

        var roles = await _userManager.GetRolesAsync(user);

        var roleEntities = await _db.Roles
            .Where(r => roles.Contains(r.Name!))
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .ToListAsync(cancellationToken);

        var permissions = roleEntities
            .SelectMany(r => r.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToList();

        var (newAccessToken, expiresAt) = _jwtService.GenerateAccessToken(
            user.Id, 
            user.Email ?? string.Empty, 
            user.FullName, 
            user.HospitalId, 
            roles, 
            permissions);

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = ipAddress
        };

        _db.RefreshTokens.Add(newRefreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        var userDto = new AuthUserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            user.StaffId,
            user.BmdcRegNo,
            user.Designation,
            user.HospitalId,
            roles,
            permissions
        );

        return new LoginResponse(newAccessToken, newRefreshTokenString, expiresAt, userDto);
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == refreshToken, cancellationToken);
        if (token == null || !token.IsActive)
        {
            return false;
        }

        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<AuthUserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new DomainException("User not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);

        var permissions = await _db.Roles
            .Where(r => roles.Contains(r.Name!))
            .SelectMany(r => r.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync(cancellationToken);

        return new AuthUserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            user.StaffId,
            user.BmdcRegNo,
            user.Designation,
            user.HospitalId,
            roles,
            permissions
        );
    }
}
