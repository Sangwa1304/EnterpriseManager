using EnterpriseManager.Domain.Licensing;
using EnterpriseManager.Domain.Memberships;
using Xunit;

namespace EnterpriseManager.Domain.Tests;

public sealed class MembershipAndLicenseTests
{
    [Fact]
    public void MembershipRequest_CanOnlyBeDecidedOnce()
    {
        var request = new MembershipRequest(Guid.NewGuid());
        request.Approve(Guid.NewGuid());

        Assert.Equal(MembershipRequestStatus.Approved, request.Status);
        Assert.Throws<InvalidOperationException>(() => request.Reject(Guid.NewGuid(), "Too late"));
    }

    [Fact]
    public void RevokedMembership_CannotBeReactivated()
    {
        var membership = new CompanyMembership(Guid.NewGuid(), "Worker");
        membership.Revoke();

        Assert.Throws<InvalidOperationException>(() => membership.Reactivate());
        Assert.Equal(MembershipStatus.Revoked, membership.Status);
    }

    [Fact]
    public void FiniteLicense_ExpiresAtItsConfiguredInstant()
    {
        var start = DateTimeOffset.UtcNow;
        var expiry = start.AddDays(30);
        var license = new UserLicense(Guid.NewGuid(), "STANDARD", start, expiry);

        Assert.True(license.IsValidAt(start.AddDays(1)));
        Assert.False(license.IsValidAt(expiry));
    }

    [Fact]
    public void LifetimeLicense_DoesNotRequireExpiry()
    {
        var license = new UserLicense(Guid.NewGuid(), "OWNER", DateTimeOffset.UtcNow, null, isLifetime: true);

        Assert.True(license.IsValidAt(DateTimeOffset.UtcNow.AddYears(50)));
    }
}
