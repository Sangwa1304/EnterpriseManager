using EnterpriseManager.Domain.Common;

namespace EnterpriseManager.Domain.Devices;

public sealed class RegisteredDevice : Entity
{
    private RegisteredDevice() { }

    public RegisteredDevice(Guid userId, Guid deviceKey, string displayName, DevicePlatform platform)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A user is required.", nameof(userId));
        if (deviceKey == Guid.Empty) throw new ArgumentException("A device key is required.", nameof(deviceKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        if (displayName.Trim().Length > 100) throw new ArgumentOutOfRangeException(nameof(displayName));
        UserId = userId;
        DeviceKey = deviceKey;
        DisplayName = displayName.Trim();
        Platform = platform;
        Status = DeviceStatus.Active;
        RegisteredAtUtc = DateTimeOffset.UtcNow;
        LastSeenAtUtc = RegisteredAtUtc;
    }

    public Guid UserId { get; private set; }
    public Guid DeviceKey { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public DevicePlatform Platform { get; private set; }
    public DeviceStatus Status { get; private set; }
    public DateTimeOffset RegisteredAtUtc { get; private set; }
    public DateTimeOffset LastSeenAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public Guid? RevokedByUserId { get; private set; }

    public void Touch(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        if (displayName.Trim().Length > 100) throw new ArgumentOutOfRangeException(nameof(displayName));
        if (Status != DeviceStatus.Active)
            throw new InvalidOperationException("A revoked device cannot be updated.");
        DisplayName = displayName.Trim();
        LastSeenAtUtc = DateTimeOffset.UtcNow;
        MarkUpdated();
    }

    public void Revoke(Guid revokedByUserId)
    {
        if (revokedByUserId == Guid.Empty) throw new ArgumentException("A revoking user is required.", nameof(revokedByUserId));
        if (Status == DeviceStatus.Revoked) return;
        Status = DeviceStatus.Revoked;
        RevokedAtUtc = DateTimeOffset.UtcNow;
        RevokedByUserId = revokedByUserId;
        MarkUpdated();
    }
}

public enum DevicePlatform { Windows = 1, Android = 2, Other = 3 }
public enum DeviceStatus { Active = 1, Revoked = 2 }
