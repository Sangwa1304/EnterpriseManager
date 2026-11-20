using EnterpriseManager.Domain.Audit;
using EnterpriseManager.Infrastructure.Persistence;

namespace EnterpriseManager.Server.Audit;

public sealed class AuditLogService(CompanyDbContext db)
{
    public void Record(Guid? actorUserId, string action, string targetType, string targetId,
        string? ipAddress = null, string? details = null)
    {
        db.AuditEvents.Add(new AuditEvent(actorUserId, action, targetType, targetId,
            DateTimeOffset.UtcNow, ipAddress, details));
    }
}
