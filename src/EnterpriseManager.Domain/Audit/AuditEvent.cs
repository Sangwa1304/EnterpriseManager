namespace EnterpriseManager.Domain.Audit;

public sealed class AuditEvent
{
    private AuditEvent() { }

    public AuditEvent(Guid? actorUserId, string action, string targetType, string targetId,
        DateTimeOffset occurredAtUtc, string? ipAddress = null, string? details = null)
    {
        if (actorUserId == Guid.Empty) throw new ArgumentException("Actor ID cannot be empty.", nameof(actorUserId));
        ActorUserId = actorUserId;
        Action = Required(action, nameof(action), 80);
        TargetType = Required(targetType, nameof(targetType), 80);
        TargetId = Required(targetId, nameof(targetId), 120);
        OccurredAtUtc = occurredAtUtc;
        IpAddress = Limit(ipAddress, 64);
        Details = Limit(details, 1000);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? ActorUserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string TargetType { get; private set; } = string.Empty;
    public string TargetId { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string? IpAddress { get; private set; }
    public string? Details { get; private set; }

    private static string Required(string value, string name, int max)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        var normalized = value.Trim();
        if (normalized.Length > max) throw new ArgumentOutOfRangeException(name);
        return normalized;
    }

    private static string? Limit(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
}
