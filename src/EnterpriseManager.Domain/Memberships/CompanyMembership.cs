using EnterpriseManager.Domain.Common;

namespace EnterpriseManager.Domain.Memberships;

public sealed class CompanyMembership : Entity
{
    private CompanyMembership() { }

    public CompanyMembership(Guid userId, string roleName)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A user is required.", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);
        if (roleName.Length > 80) throw new ArgumentOutOfRangeException(nameof(roleName));
        UserId = userId;
        RoleName = roleName.Trim();
        Status = MembershipStatus.Active;
    }

    public Guid UserId { get; private set; }
    public string RoleName { get; private set; } = string.Empty;
    public MembershipStatus Status { get; private set; }
    public DateTimeOffset JoinedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SuspendedAtUtc { get; private set; }

    public void ChangeRole(string roleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);
        if (roleName.Length > 80) throw new ArgumentOutOfRangeException(nameof(roleName));
        RoleName = roleName.Trim();
        MarkUpdated();
    }

    public void Suspend()
    {
        Status = MembershipStatus.Suspended;
        SuspendedAtUtc = DateTimeOffset.UtcNow;
        MarkUpdated();
    }

    public void Reactivate()
    {
        if (Status == MembershipStatus.Revoked)
            throw new InvalidOperationException("A revoked membership cannot be reactivated.");
        Status = MembershipStatus.Active;
        SuspendedAtUtc = null;
        MarkUpdated();
    }

    public void Revoke()
    {
        Status = MembershipStatus.Revoked;
        SuspendedAtUtc = DateTimeOffset.UtcNow;
        MarkUpdated();
    }
}

public enum MembershipStatus { Active = 1, Suspended = 2, Revoked = 3 }
