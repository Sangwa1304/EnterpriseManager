using EnterpriseManager.Domain.Common;

namespace EnterpriseManager.Domain.Companies;

public sealed class Company : Entity
{
    private Company() { }

    public Company(string legalName, string businessIdentifier, string timeZone)
    {
        LegalName = Required(legalName, nameof(legalName), 200);
        BusinessIdentifier = Required(businessIdentifier, nameof(businessIdentifier), 80);
        TimeZone = Required(timeZone, nameof(timeZone), 100);
        Status = CompanyStatus.Active;
    }

    public string LegalName { get; private set; } = string.Empty;
    public string BusinessIdentifier { get; private set; } = string.Empty;
    public string TimeZone { get; private set; } = "UTC";
    public CompanyStatus Status { get; private set; }

    public void Suspend() { Status = CompanyStatus.Suspended; MarkUpdated(); }
    public void Activate() { Status = CompanyStatus.Active; MarkUpdated(); }

    private static string Required(string value, string name, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentOutOfRangeException(name, $"Maximum length is {maxLength}.");
        return normalized;
    }
}

public enum CompanyStatus { Active = 1, Suspended = 2, Closed = 3 }
