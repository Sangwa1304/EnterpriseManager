using EnterpriseManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var connectionString = Environment.GetEnvironmentVariable("ENTERPRISEMANAGER_DB_CONNECTION");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Missing ENTERPRISEMANAGER_DB_CONNECTION. No database was changed.");
    return 2;
}

var options = new DbContextOptionsBuilder<CompanyDbContext>()
    .UseSqlite(connectionString, sqlite =>
        sqlite.MigrationsAssembly(typeof(CompanyDbContext).Assembly.FullName))
    .Options;

await using var db = new CompanyDbContext(options);
var migrations = db.Database.GetMigrations();
if (!migrations.Any())
{
    Console.Error.WriteLine(
        "No EF Core migrations are present. Provisioning stopped without creating or modifying the database.");
    Console.Error.WriteLine(
        "Generate and review the initial CompanyDbContext migration before running this tool.");
    return 3;
}

var pending = (await db.Database.GetPendingMigrationsAsync()).ToArray();
if (pending.Length == 0)
{
    Console.WriteLine("The company database schema is already up to date.");
    return 0;
}

Console.WriteLine($"Applying {pending.Length} schema migration(s) to the configured central database...");
await db.Database.MigrateAsync();
Console.WriteLine("Database schema migration completed.");
return 0;
