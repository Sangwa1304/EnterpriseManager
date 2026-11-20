using EnterpriseManager.Domain.Common;

namespace EnterpriseManager.Domain.Licensing;

public sealed class UserLicense : Entity
{
    private UserLicense() { }

    public UserLicense(Guid userId, string planCode, DateTimeOffset startsAtUtc, DateTimeOffset? expiresAtUtc, bool isLifetime = false)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A user is required.", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(planCode);
        if (planCode.Length > 40) throw new ArgumentOutOfRangeException(nameof(planCode));
        if (!isLifetime && (expiresAtUtc is null || expiresAtUtc <= startsAtUtc))
            throw new ArgumentException("A finite license must expire after its start date.", nameof(expiresAtUtc));
        if (isLifetime && expiresAtUtc is not null)
            throw new ArgumentException("A lifetime license cannot have an expiry date.", nameof(expiresAtUtc));
        UserId = userId;
        PlanCode = planCode.Trim().ToUpperInvariant();
        StartsAtUtc = startsAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        IsLifetime = isLifetime;
        Status = LicenseStatus.Active;
    }

    public Guid UserId { get; private set; }
    public string PlanCode { get; private set; } = string.Empty;
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public bool IsLifetime { get; private set; }
    public LicenseStatus Status { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public bool IsValidAt(DateTimeOffset instant) =>
        Status == LicenseStatus.Active && StartsAtUtc <= instant &&
        (IsLifetime || ExpiresAtUtc > instant);

    public void Revoke()
    {
        Status = LicenseStatus.Revoked;
        RevokedAtUtc = DateTimeOffset.UtcNow;
        MarkUpdated();
    }

    public void Suspend()
    {
        Status = LicenseStatus.Suspended;
        MarkUpdated();
    }
}

public enum LicenseStatus { Active = 1, Suspended = 2, Revoked = 3 }
