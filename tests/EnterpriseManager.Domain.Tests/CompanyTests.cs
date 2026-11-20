using EnterpriseManager.Domain.Companies;

namespace EnterpriseManager.Domain.Tests;

public sealed class CompanyTests
{
    [Fact]
    public void Constructor_NormalizesCompanyName_AndStartsActive()
    {
        var company = new Company("  Multi Vision  ", " MV-001 ", "Africa/Kinshasa");

        Assert.Equal("Multi Vision", company.LegalName);
        Assert.Equal("MV-001", company.BusinessIdentifier);
        Assert.Equal(CompanyStatus.Active, company.Status);
    }

    [Fact]
    public void Constructor_RejectsBlankLegalName()
    {
        Assert.Throws<ArgumentException>(() =>
            new Company(" ", "MV-001", "Africa/Kinshasa"));
    }

    [Fact]
    public void Suspend_ChangesCompanyStatus()
    {
        var company = new Company("Multi Vision", "MV-001", "Africa/Kinshasa");

        company.Suspend();

        Assert.Equal(CompanyStatus.Suspended, company.Status);
    }
}
