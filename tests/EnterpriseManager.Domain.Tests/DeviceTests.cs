using EnterpriseManager.Domain.Devices;
using Xunit;

namespace EnterpriseManager.Domain.Tests;

public sealed class DeviceTests
{
    [Fact]
    public void NewDevice_IsActiveAndHasRegisteredTimestamp()
    {
        var device = new RegisteredDevice(Guid.NewGuid(), Guid.NewGuid(), "Office PC", DevicePlatform.Windows);

        Assert.Equal(DeviceStatus.Active, device.Status);
        Assert.Equal(device.RegisteredAtUtc, device.LastSeenAtUtc);
    }

    [Fact]
    public void RevokedDevice_CannotBeTouchedAgain()
    {
        var device = new RegisteredDevice(Guid.NewGuid(), Guid.NewGuid(), "Lost phone", DevicePlatform.Android);
        device.Revoke(Guid.NewGuid());

        Assert.Equal(DeviceStatus.Revoked, device.Status);
        Assert.Throws<InvalidOperationException>(() => device.Touch("Replacement name"));
    }
}
