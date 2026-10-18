namespace Hms.Domain.Common;

/// <summary>
/// Base entity with UUID primary key, audit fields, and soft delete.
/// All domain entities inherit from this.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;
}

/// <summary>
/// Base entity with optimistic concurrency via RowVersion.
/// Used for money-related and bed-related tables.
/// </summary>
public abstract class ConcurrentEntity : BaseEntity
{
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// Base entity for hospital-scoped entities (multi-tenant ready).
/// </summary>
public abstract class HospitalScopedEntity : BaseEntity
{
    public Guid HospitalId { get; set; }
}

/// <summary>
/// Base entity for hospital-scoped entities with concurrency control.
/// </summary>
public abstract class HospitalScopedConcurrentEntity : ConcurrentEntity
{
    public Guid HospitalId { get; set; }
}
