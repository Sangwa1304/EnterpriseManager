using EnterpriseManager.Domain.Common;

namespace EnterpriseManager.Domain.Users;

public sealed class UserAccount : Entity
{
    private UserAccount() { }

    public UserAccount(string email, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        Email = email.Trim().ToLowerInvariant();
        DisplayName = displayName.Trim();
        if (Email.Length > 254 || DisplayName.Length > 160)
            throw new ArgumentOutOfRangeException(nameof(email), "User identity field is too long.");
        Status = UserStatus.Pending;
    }

    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public UserStatus Status { get; private set; }

    public void Activate() { Status = UserStatus.Active; MarkUpdated(); }
    public void Suspend() { Status = UserStatus.Suspended; MarkUpdated(); }
}

public enum UserStatus { Pending = 1, Active = 2, Suspended = 3, Revoked = 4 }
