using System.Text.Json;
using Hms.Application.Common.Interfaces;
using Hms.Domain.Entities.Audit;
using Hms.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;

namespace Hms.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly HmsDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(HmsDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        string action, 
        string entity, 
        string entityId, 
        object? oldValues = null, 
        object? newValues = null, 
        CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var userIdClaim = httpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? httpContext?.User?.FindFirst("sub")?.Value;
        Guid? userId = Guid.TryParse(userIdClaim, out var parsedGuid) ? parsedGuid : null;

        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            OldValuesJson = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValuesJson = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
            CorrelationId = httpContext?.TraceIdentifier
        };

        _db.AuditLogs.Add(auditLog);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
