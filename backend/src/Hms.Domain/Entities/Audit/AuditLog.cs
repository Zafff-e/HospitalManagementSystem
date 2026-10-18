using Hms.Domain.Common;

namespace Hms.Domain.Entities.Audit;

/// <summary>
/// Audit log entry capturing every state change to sensitive hospital data.
/// </summary>
public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty; // Create, Update, Delete, Refund, Prescribe, Verify
    public string Entity { get; set; } = string.Empty; // Patient, Invoice, Prescription, etc.
    public string EntityId { get; set; } = string.Empty;
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }
}
