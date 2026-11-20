using EnterpriseManager.Domain.Common;

namespace EnterpriseManager.Domain.Memberships;

public sealed class MembershipRequest : Entity
{
    private MembershipRequest() { }

    public MembershipRequest(Guid userId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A user is required.", nameof(userId));
        UserId = userId;
        Status = MembershipRequestStatus.Pending;
    }

    public Guid UserId { get; private set; }
    public MembershipRequestStatus Status { get; private set; }
    public DateTimeOffset RequestedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DecidedAtUtc { get; private set; }
    public Guid? DecidedByUserId { get; private set; }
    public string? RejectionReason { get; private set; }

    public void Approve(Guid directorUserId)
    {
        EnsurePending();
        if (directorUserId == Guid.Empty) throw new ArgumentException("A Director is required.", nameof(directorUserId));
        Status = MembershipRequestStatus.Approved;
        DecidedByUserId = directorUserId;
        DecidedAtUtc = DateTimeOffset.UtcNow;
        MarkUpdated();
    }

    public void Reject(Guid directorUserId, string? reason)
    {
        EnsurePending();
        if (directorUserId == Guid.Empty) throw new ArgumentException("A Director is required.", nameof(directorUserId));
        if (reason?.Length > 500) throw new ArgumentOutOfRangeException(nameof(reason));
        Status = MembershipRequestStatus.Rejected;
        DecidedByUserId = directorUserId;
        DecidedAtUtc = DateTimeOffset.UtcNow;
        RejectionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        MarkUpdated();
    }

    private void EnsurePending()
    {
        if (Status != MembershipRequestStatus.Pending)
            throw new InvalidOperationException("Only pending requests can be decided.");
    }
}

public enum MembershipRequestStatus { Pending = 1, Approved = 2, Rejected = 3 }
