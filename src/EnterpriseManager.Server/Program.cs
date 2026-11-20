using EnterpriseManager.Application;
using EnterpriseManager.Infrastructure;
using EnterpriseManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());

var connectionString = builder.Configuration.GetConnectionString("CompanyDatabase")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:CompanyDatabase must be configured for this company server.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<CompanyDbContext>("company-database");

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.Run();

public partial class Program
{
}
