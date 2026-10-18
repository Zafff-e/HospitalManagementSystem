using Microsoft.AspNetCore.Identity;

namespace Hms.Infrastructure.Identity;

/// <summary>
/// Extended ApplicationUser with hospital context, BMDC registration, and staff details.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public string? StaffId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? BmdcRegNo { get; set; } // Required for Doctor users
    public string? Designation { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid HospitalId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

/// <summary>
/// Application role with description.
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// Granular permission item (e.g. patient.read, billing.refund, emr.prescribe).
/// </summary>
public class Permission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty; // e.g. "billing.refund"
    public string Category { get; set; } = string.Empty; // e.g. "Billing"
    public string Description { get; set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// Mapping between Roles and Permissions.
/// </summary>
public class RolePermission
{
    public Guid RoleId { get; set; }
    public ApplicationRole Role { get; set; } = null!;

    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}

/// <summary>
/// Rotating refresh token for JWT renewal.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByIp { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;
}
