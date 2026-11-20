using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using EnterpriseManager.Application;
using EnterpriseManager.Infrastructure;
using EnterpriseManager.Infrastructure.Persistence;
using EnterpriseManager.Infrastructure.Identity;
using EnterpriseManager.Domain.Devices;
using EnterpriseManager.Server.Authentication;
using EnterpriseManager.Server.Memberships;
using EnterpriseManager.Server.Devices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());

var connectionString = builder.Configuration.GetConnectionString("CompanyDatabase")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:CompanyDatabase must be configured for this company server.");

var tokenOptions = builder.Configuration.GetSection(TokenOptions.SectionName).Get<TokenOptions>()
    ?? throw new InvalidOperationException("Authentication:Tokens configuration is required.");
if (string.IsNullOrWhiteSpace(tokenOptions.SigningKey) ||
    Encoding.UTF8.GetByteCount(tokenOptions.SigningKey) < 32 ||
    string.IsNullOrWhiteSpace(tokenOptions.Issuer) ||
    string.IsNullOrWhiteSpace(tokenOptions.Audience) ||
    tokenOptions.AccessTokenMinutes is < 5 or > 60 ||
    tokenOptions.RefreshTokenDays is < 1 or > 30)
    throw new InvalidOperationException(
        "Configure JWT SigningKey (32+ UTF-8 bytes), Issuer and Audience outside source control.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.Configure<TokenOptions>(builder.Configuration.GetSection(TokenOptions.SectionName));
builder.Services.AddScoped<TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = tokenOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = tokenOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                var userIdValue = principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    context.Fail("The access token does not identify a valid account.");
                    return;
                }

                var deviceIdValue = principal?.FindFirst("device_id")?.Value;
                if (!Guid.TryParse(deviceIdValue, out var deviceId))
                {
                    context.Fail("The access token is not bound to a registered device.");
                    return;
                }

                var services = context.HttpContext.RequestServices;
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                var db = services.GetRequiredService<CompanyDbContext>();
                var user = await userManager.FindByIdAsync(userId.ToString());
                var device = await db.Devices.SingleOrDefaultAsync(
                    x => x.Id == deviceId && x.UserId == userId, context.HttpContext.RequestAborted);
                if (user is null || !user.IsActive || device is null || device.Status != DeviceStatus.Active ||
                    !await AuthEndpoints.HasActiveMembershipAndLicenseAsync(
                        db, userId, context.HttpContext.RequestAborted))
                {
                    context.Fail("The account, membership or license is inactive.");
                    return;
                }

                if (principal?.Identity is ClaimsIdentity identity)
                {
                    foreach (var existingRole in identity.FindAll(ClaimTypes.Role).ToArray())
                        identity.RemoveClaim(existingRole);
                    foreach (var role in await userManager.GetRolesAsync(user))
                        identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }
            }
        };
    });
builder.Services.AddAuthorization(options =>
    options.AddPolicy("DirectorOnly", policy => policy.RequireRole("Director")));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("authentication", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<CompanyDbContext>("company-database");

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapAuthenticationEndpoints();
app.MapMembershipEndpoints();
app.MapDeviceEndpoints();

app.Run();

public partial class Program
{
}
