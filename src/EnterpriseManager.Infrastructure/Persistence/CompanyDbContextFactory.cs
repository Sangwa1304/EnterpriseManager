using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EnterpriseManager.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by EF Core tooling. The connection string is deliberately
/// supplied through the environment so secrets never need to be committed or passed
/// as command-line arguments.
/// </summary>
public sealed class CompanyDbContextFactory : IDesignTimeDbContextFactory<CompanyDbContext>
{
    public CompanyDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ENTERPRISEMANAGER_DB_CONNECTION")
            ?? throw new InvalidOperationException(
                "Set ENTERPRISEMANAGER_DB_CONNECTION to the central company SQLite connection string.");

        var options = new DbContextOptionsBuilder<CompanyDbContext>()
            .UseSqlite(connectionString, sqlite =>
                sqlite.MigrationsAssembly(typeof(CompanyDbContext).Assembly.FullName))
            .Options;

        return new CompanyDbContext(options);
    }
}
