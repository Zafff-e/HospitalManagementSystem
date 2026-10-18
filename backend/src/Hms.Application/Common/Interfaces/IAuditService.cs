namespace Hms.Application.Common.Interfaces;

public interface IAuditService
{
    Task LogAsync(string action, string entity, string entityId, object? oldValues = null, object? newValues = null, CancellationToken cancellationToken = default);
}
