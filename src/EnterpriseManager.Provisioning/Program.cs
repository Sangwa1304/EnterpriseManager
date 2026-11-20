using EnterpriseManager.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

const int InvalidArguments = 64;
const int MissingConfiguration = 2;
const int MissingMigrations = 3;
const int IntegrityFailure = 5;

var apply = args.Length == 1 && args[0] == "--apply";
var preview = args.Length == 0 || (args.Length == 1 && args[0] == "--status");
if (!apply && !preview)
{
    Console.Error.WriteLine("Usage: EnterpriseManager.Provisioning [--status|--apply]");
    return InvalidArguments;
}

var rawConnectionString = Environment.GetEnvironmentVariable("ENTERPRISEMANAGER_DB_CONNECTION");
if (string.IsNullOrWhiteSpace(rawConnectionString))
{
    Console.Error.WriteLine("Missing ENTERPRISEMANAGER_DB_CONNECTION. No database was changed.");
    return MissingConfiguration;
}

SqliteConnectionStringBuilder connection;
try
{
    connection = new SqliteConnectionStringBuilder(rawConnectionString);
}
catch (ArgumentException)
{
    Console.Error.WriteLine("The configured SQLite connection string is invalid. No database was changed.");
    return MissingConfiguration;
}

if (connection.Mode == SqliteOpenMode.Memory ||
    string.IsNullOrWhiteSpace(connection.DataSource) ||
    connection.DataSource == ":memory:" ||
    !Path.IsPathFullyQualified(connection.DataSource))
{
    Console.Error.WriteLine(
        "Provisioning requires an absolute, file-backed SQLite path for the central company database.");
    return MissingConfiguration;
}

if (apply && connection.Mode == SqliteOpenMode.ReadOnly)
{
    Console.Error.WriteLine("The configured SQLite connection is read-only. No database was changed.");
    return MissingConfiguration;
}

var databasePath = Path.GetFullPath(connection.DataSource);
connection.DataSource = databasePath;
var databaseExists = File.Exists(databasePath);

var migrationsOptions = new DbContextOptionsBuilder<CompanyDbContext>();
if (preview && databaseExists)
    connection.Mode = SqliteOpenMode.ReadOnly;

migrationsOptions.UseSqlite(connection.ToString(), sqlite =>
    sqlite.MigrationsAssembly(typeof(CompanyDbContext).Assembly.FullName));

await using var db = new CompanyDbContext(migrationsOptions.Options);
var migrations = db.Database.GetMigrations().ToArray();
if (migrations.Length == 0)
{
    Console.Error.WriteLine(
        "No EF Core migrations are present. Provisioning stopped without creating or modifying the database.");
    Console.Error.WriteLine(
        "Generate and review the initial CompanyDbContext migration before running this tool.");
    return MissingMigrations;
}

var pending = preview && !databaseExists
    ? migrations
    : (await db.Database.GetPendingMigrationsAsync()).ToArray();

if (pending.Length == 0)
{
    Console.WriteLine("The company database schema is already up to date.");
    return 0;
}

Console.WriteLine($"Pending migration(s): {string.Join(", ", pending)}");
if (preview)
{
    Console.WriteLine("Preview only. No database changes were made. Use --apply to apply migrations.");
    return 0;
}

Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
string? backupPath = null;
if (databaseExists && new FileInfo(databasePath).Length > 0)
{
    backupPath = databasePath + "." +
        DateTimeOffset.UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'") +
        ".pre-migration.bak";

    var sourceBuilder = new SqliteConnectionStringBuilder { DataSource = databasePath };
    var destinationBuilder = new SqliteConnectionStringBuilder { DataSource = backupPath };
    await using var source = new SqliteConnection(sourceBuilder.ToString());
    await using var destination = new SqliteConnection(destinationBuilder.ToString());
    await source.OpenAsync();
    await destination.OpenAsync();
    source.BackupDatabase(destination);
    Console.WriteLine($"Pre-migration backup created: {backupPath}");
}

Console.WriteLine($"Applying {pending.Length} migration(s) to the central company database...");
await db.Database.MigrateAsync();

await db.Database.OpenConnectionAsync();
try
{
    await using var command = db.Database.GetDbConnection().CreateCommand();
    command.CommandText = "PRAGMA integrity_check;";
    var integrity = Convert.ToString(await command.ExecuteScalarAsync());
    if (!string.Equals(integrity, "ok", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine($"SQLite integrity check failed: {integrity ?? "no result"}");
        if (backupPath is not null)
            Console.Error.WriteLine($"The pre-migration backup is preserved at: {backupPath}");
        return IntegrityFailure;
    }
}
finally
{
    await db.Database.CloseConnectionAsync();
}

Console.WriteLine("Migrations applied and SQLite integrity check passed.");
return 0;
