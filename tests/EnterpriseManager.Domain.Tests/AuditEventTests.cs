using EnterpriseManager.Domain.Audit;
using Xunit;

namespace EnterpriseManager.Domain.Tests;

public sealed class AuditEventTests
{
    [Fact]
    public void AnonymousSecurityEvent_AllowsNullActorAndNormalizesValues()
    {
        var audit = new AuditEvent(
            null, " security.login-failed ", " account ", " user-123 ",
            DateTimeOffset.UtcNow, " 127.0.0.1 ", " Invalid credentials ");

        Assert.Null(audit.ActorUserId);
        Assert.Equal("security.login-failed", audit.Action);
        Assert.Equal("account", audit.TargetType);
        Assert.Equal("user-123", audit.TargetId);
        Assert.Equal("127.0.0.1", audit.IpAddress);
        Assert.Equal("Invalid credentials", audit.Details);
    }

    [Fact]
    public void Event_RejectsEmptyAction()
    {
        Assert.Throws<ArgumentException>(() =>
            new AuditEvent(Guid.NewGuid(), " ", "account", "user-123", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Event_RejectsValuesBeyondConfiguredLength()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AuditEvent(Guid.NewGuid(), new string('a', 81), "account", "user-123", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Event_TruncatesOptionalIpAndDetails()
    {
        var audit = new AuditEvent(
            Guid.NewGuid(), "security.test", "account", "user-123",
            DateTimeOffset.UtcNow, new string('1', 70), new string('d', 1100));

        Assert.Equal(64, audit.IpAddress!.Length);
        Assert.Equal(1000, audit.Details!.Length);
    }
}
