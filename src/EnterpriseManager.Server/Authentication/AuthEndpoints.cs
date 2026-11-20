using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EnterpriseManager.Domain.Companies;
using EnterpriseManager.Infrastructure.Identity;
using EnterpriseManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManager.Server.Authentication;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth").WithTags("Authentication");
        group.MapPost("/login", LoginAsync).AllowAnonymous().RequireRateLimiting("authentication");
        group.MapPost("/refresh", RefreshAsync).AllowAnonymous().RequireRateLimiting("authentication");
        group.MapPost("/setup/director", SetupAsync).AllowAnonymous().RequireRateLimiting("authentication");
        group.MapGet("/me", (ClaimsPrincipal principal) => Results.Ok(new CurrentUserResponse(
            principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            principal.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray()))).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, SignInManager<ApplicationUser> signIn,
        UserManager<ApplicationUser> users, TokenService tokens, HttpContext http, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["credentials"] = ["Email and password are required."] });
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive) return Results.Unauthorized();
        var check = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!check.Succeeded) return Results.Unauthorized();
        return Results.Ok(await tokens.CreateAsync(user, http.Connection.RemoteIpAddress?.ToString(), ct));
    }

    private static async Task<IResult> RefreshAsync(RefreshRequest request, TokenService tokens, HttpContext http, CancellationToken ct)
    {
        var pair = await tokens.RefreshAsync(request.RefreshToken, http.Connection.RemoteIpAddress?.ToString(), ct);
        return pair is null ? Results.Unauthorized() : Results.Ok(pair);
    }

    private static async Task<IResult> SetupAsync(SetupCompanyRequest request, IConfiguration config,
        CompanyDbContext db, UserManager<ApplicationUser> users, RoleManager<IdentityRole<Guid>> roles,
        HttpContext http, CancellationToken ct)
    {
        var expected = config["Bootstrap:SetupKey"];
        if (string.IsNullOrWhiteSpace(expected)) return Results.NotFound();
        var supplied = http.Request.Headers["X-Enterprise-Setup-Key"].ToString();
        if (!FixedEquals(expected, supplied)) return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.LegalName) || request.LegalName.Length > 200 ||
            string.IsNullOrWhiteSpace(request.BusinessIdentifier) || request.BusinessIdentifier.Length > 80 ||
            string.IsNullOrWhiteSpace(request.TimeZone) || request.TimeZone.Length > 100 ||
            string.IsNullOrWhiteSpace(request.DirectorEmail) || request.DirectorEmail.Length > 254 ||
            string.IsNullOrWhiteSpace(request.DirectorName) || request.DirectorName.Length > 160 ||
            string.IsNullOrWhiteSpace(request.DirectorPassword) || request.DirectorPassword.Length > 256)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["setup"] = ["All company and Director fields are required."] });

        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        if (await db.Companies.AnyAsync(ct) || await users.Users.AnyAsync(ct))
            return Results.Conflict(new { message = "Company setup has already been completed." });
        var role = await roles.FindByNameAsync("Director");
        if (role is null)
        {
            var roleResult = await roles.CreateAsync(new IdentityRole<Guid> { Name = "Director", NormalizedName = "DIRECTOR" });
            if (!roleResult.Succeeded) return Results.Problem("Could not initialize the Director role.");
        }

        var director = new ApplicationUser
        {
            Id = Guid.NewGuid(), UserName = request.DirectorEmail.Trim(), Email = request.DirectorEmail.Trim(),
            DisplayName = request.DirectorName.Trim(), IsActive = true, EmailConfirmed = false
        };
        var created = await users.CreateAsync(director, request.DirectorPassword);
        if (!created.Succeeded)
            return Results.ValidationProblem(created.Errors.GroupBy(x => x.Code)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Description).ToArray()));
        var assigned = await users.AddToRoleAsync(director, "Director");
        if (!assigned.Succeeded) return Results.Problem("Could not assign the initial Director role.");

        var company = new Company(request.LegalName, request.BusinessIdentifier, request.TimeZone);
        db.Companies.Add(company);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Results.Created("/api/v1/auth/setup/director", new CompanySetupResponse(company.Id, company.LegalName, director.Id));
    }

    private static bool FixedEquals(string expected, string supplied)
    {
        var a = Encoding.UTF8.GetBytes(expected);
        var b = Encoding.UTF8.GetBytes(supplied);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }
}
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record SetupCompanyRequest(string LegalName, string BusinessIdentifier, string TimeZone,
    string DirectorEmail, string DirectorName, string DirectorPassword);
public sealed record CompanySetupResponse(Guid CompanyId, string CompanyName, Guid DirectorUserId);
public sealed record CurrentUserResponse(string UserId, string DisplayName, string Email, string[] Roles);
