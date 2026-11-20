using System.Security.Claims;
using EnterpriseManager.Domain.Licensing;
using EnterpriseManager.Domain.Memberships;
using EnterpriseManager.Infrastructure.Identity;
using EnterpriseManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManager.Server.Memberships;

public static class MembershipEndpoints
{
    private static readonly HashSet<string> AssignableRoles =
        ["Worker", "Manager", "Finance", "Construction", "Production"];
    private static readonly HashSet<string> LicensePlans =
        ["STANDARD", "PRO", "ENTERPRISE"];

    public static IEndpointRouteBuilder MapMembershipEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/membership").WithTags("Membership");
        group.MapPost("/requests", SubmitRequestAsync)
            .AllowAnonymous()
            .RequireRateLimiting("authentication");

        group.MapGet("/requests/pending", GetPendingRequestsAsync)
            .RequireAuthorization("DirectorOnly");

        group.MapPost("/requests/{requestId:guid}/approve", ApproveRequestAsync)
            .RequireAuthorization("DirectorOnly");

        group.MapPost("/requests/{requestId:guid}/reject", RejectRequestAsync)
            .RequireAuthorization("DirectorOnly");

        group.MapPost("/users/{userId:guid}/licenses", IssueLicenseAsync)
            .RequireAuthorization("DirectorOnly");

        group.MapPost("/users/{userId:guid}/suspend", SuspendMembershipAsync)
            .RequireAuthorization("DirectorOnly");

        group.MapPost("/users/{userId:guid}/reactivate", ReactivateMembershipAsync)
            .RequireAuthorization("DirectorOnly");

        group.MapPost("/users/{userId:guid}/revoke", RevokeMembershipAsync)
            .RequireAuthorization("DirectorOnly");

        return endpoints;
    }

    private static async Task<IResult> SubmitRequestAsync(
        JoinRequestInput input,
        CompanyDbContext db,
        UserManager<ApplicationUser> users,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Email) || input.Email.Length > 254 ||
            string.IsNullOrWhiteSpace(input.DisplayName) || input.DisplayName.Length > 160 ||
            string.IsNullOrWhiteSpace(input.Password) || input.Password.Length > 256)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["A valid email, display name and password are required."]
            });

        if (!await db.Companies.AnyAsync(ct))
            return Results.Conflict(new { message = "This company server has not been initialized." });

        var email = input.Email.Trim();
        if (await users.FindByEmailAsync(email) is not null)
            return Results.Accepted(value: new { message = "If the address can be used, the request will be reviewed by the Director." });

        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, ct);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = input.DisplayName.Trim(),
            IsActive = false,
            EmailConfirmed = false
        };

        var createResult = await users.CreateAsync(user, input.Password);
        if (!createResult.Succeeded)
            return Results.ValidationProblem(createResult.Errors
                .GroupBy(x => x.Code)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Description).ToArray()));

        var request = new MembershipRequest(user.Id);
        db.MembershipRequests.Add(request);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return Results.Accepted($"/api/v1/membership/requests/{request.Id}",
            new { request.Id, request.Status, request.RequestedAtUtc });
    }

    private static async Task<IResult> GetPendingRequestsAsync(
        CompanyDbContext db,
        UserManager<ApplicationUser> users,
        CancellationToken ct)
    {
        var requests = await db.MembershipRequests
            .Where(x => x.Status == MembershipRequestStatus.Pending)
            .OrderBy(x => x.RequestedAtUtc)
            .ToListAsync(ct);

        var results = new List<PendingMembershipRequest>();
        foreach (var request in requests)
        {
            var user = await users.FindByIdAsync(request.UserId.ToString());
            if (user is null) continue;
            results.Add(new PendingMembershipRequest(
                request.Id, user.Id, user.Email ?? string.Empty,
                user.DisplayName, request.RequestedAtUtc));
        }

        return Results.Ok(results);
    }

    private static async Task<IResult> ApproveRequestAsync(
        Guid requestId,
        ApproveMembershipInput input,
        ClaimsPrincipal principal,
        CompanyDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole<Guid>> roles,
        CancellationToken ct)
    {
        if (!AssignableRoles.Contains(input.RoleName) ||
            !LicensePlans.Contains(input.LicensePlanCode) ||
            input.LicenseDays is < 1 or > 3650)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["approval"] = ["Choose an assignable role, a supported license plan and a duration from 1 to 3650 days."]
            });

        var directorId = GetUserId(principal);
        if (directorId is null) return Results.Unauthorized();

        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, ct);

        var request = await db.MembershipRequests.SingleOrDefaultAsync(x => x.Id == requestId, ct);
        if (request is null) return Results.NotFound();
        if (request.Status != MembershipRequestStatus.Pending)
            return Results.Conflict(new { message = "This request has already been decided." });

        var user = await users.FindByIdAsync(request.UserId.ToString());
        if (user is null) return Results.Conflict(new { message = "The requesting account no longer exists." });
        if (await db.CompanyMemberships.AnyAsync(x => x.UserId == user.Id, ct))
            return Results.Conflict(new { message = "This account already has a company membership." });

        if (await roles.FindByNameAsync(input.RoleName) is null)
            return Results.BadRequest(new { message = "The selected role is not configured for this company." });

        request.Approve(directorId.Value);
        user.IsActive = true;
        var updateResult = await users.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return Results.Problem("Could not activate the approved account.");

        var roleResult = await users.AddToRoleAsync(user, input.RoleName);
        if (!roleResult.Succeeded)
            return Results.Problem("Could not assign the approved role.");

        var now = DateTimeOffset.UtcNow;
        db.CompanyMemberships.Add(new CompanyMembership(user.Id, input.RoleName));
        db.UserLicenses.Add(new UserLicense(
            user.Id, input.LicensePlanCode, now, now.AddDays(input.LicenseDays)));

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return Results.Ok(new MembershipDecisionResponse(
            request.Id, user.Id, input.RoleName, input.LicensePlanCode,
            now.AddDays(input.LicenseDays)));
    }

    private static async Task<IResult> RejectRequestAsync(
        Guid requestId,
        RejectMembershipInput input,
        ClaimsPrincipal principal,
        CompanyDbContext db,
        CancellationToken ct)
    {
        var directorId = GetUserId(principal);
        if (directorId is null) return Results.Unauthorized();

        var request = await db.MembershipRequests.SingleOrDefaultAsync(x => x.Id == requestId, ct);
        if (request is null) return Results.NotFound();
        if (request.Status != MembershipRequestStatus.Pending)
            return Results.Conflict(new { message = "This request has already been decided." });

        request.Reject(directorId.Value, input.Reason);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { request.Id, request.Status, request.DecidedAtUtc });
    }

    private static async Task<IResult> IssueLicenseAsync(
        Guid userId,
        IssueLicenseInput input,
        CompanyDbContext db,
        UserManager<ApplicationUser> users,
        CancellationToken ct)
    {
        if (!LicensePlans.Contains(input.PlanCode) || input.DurationDays is < 1 or > 3650)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["license"] = ["Choose a supported plan and a duration from 1 to 3650 days."]
            });

        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null) return Results.NotFound();
        var membership = await db.CompanyMemberships.SingleOrDefaultAsync(x => x.UserId == userId, ct);
        if (membership is null || membership.Status != MembershipStatus.Active)
            return Results.Conflict(new { message = "An active company membership is required." });

        var now = DateTimeOffset.UtcNow;
        var license = new UserLicense(userId, input.PlanCode, now, now.AddDays(input.DurationDays));
        db.UserLicenses.Add(license);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/membership/users/{userId}/licenses",
            new { license.Id, license.PlanCode, license.StartsAtUtc, license.ExpiresAtUtc });
    }

    private static async Task<IResult> SuspendMembershipAsync(
        Guid userId,
        ClaimsPrincipal principal,
        CompanyDbContext db,
        UserManager<ApplicationUser> users,
        CancellationToken ct)
    {
        if (GetUserId(principal) == userId)
            return Results.Conflict(new { message = "The Director cannot suspend their own account here." });

        var membership = await db.CompanyMemberships.SingleOrDefaultAsync(x => x.UserId == userId, ct);
        var user = await users.FindByIdAsync(userId.ToString());
        if (membership is null || user is null) return Results.NotFound();

        membership.Suspend();
        user.IsActive = false;
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded) return Results.Problem("Could not suspend the account.");

        await RevokeRefreshTokensAsync(db, userId, ct);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ReactivateMembershipAsync(
        Guid userId,
        CompanyDbContext db,
        UserManager<ApplicationUser> users,
        CancellationToken ct)
    {
        var membership = await db.CompanyMemberships.SingleOrDefaultAsync(x => x.UserId == userId, ct);
        var user = await users.FindByIdAsync(userId.ToString());
        if (membership is null || user is null) return Results.NotFound();
        if (membership.Status == MembershipStatus.Revoked)
            return Results.Conflict(new { message = "A revoked membership cannot be reactivated." });

        membership.Reactivate();
        user.IsActive = true;
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded) return Results.Problem("Could not reactivate the account.");
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { user.Id, membership.Status });
    }

    private static async Task<IResult> RevokeMembershipAsync(
        Guid userId,
        ClaimsPrincipal principal,
        CompanyDbContext db,
        UserManager<ApplicationUser> users,
        CancellationToken ct)
    {
        if (GetUserId(principal) == userId)
            return Results.Conflict(new { message = "The Director cannot revoke their own membership here." });

        var membership = await db.CompanyMemberships.SingleOrDefaultAsync(x => x.UserId == userId, ct);
        var user = await users.FindByIdAsync(userId.ToString());
        if (membership is null || user is null) return Results.NotFound();

        membership.Revoke();
        user.IsActive = false;
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded) return Results.Problem("Could not revoke the account.");
        var licenses = await db.UserLicenses.Where(x => x.UserId == userId).ToListAsync(ct);
        foreach (var license in licenses)
            if (license.Status == LicenseStatus.Active) license.Revoke();

        await RevokeRefreshTokensAsync(db, userId, ct);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task RevokeRefreshTokensAsync(CompanyDbContext db, Guid userId, CancellationToken ct)
    {
        var activeTokens = await db.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAtUtc == null)
            .ToListAsync(ct);
        var now = DateTimeOffset.UtcNow;
        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = now;
            token.RevokedByIp = "director-action";
            token.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        }
    }

    private static Guid? GetUserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}

public sealed record JoinRequestInput(string Email, string DisplayName, string Password);
public sealed record ApproveMembershipInput(string RoleName, string LicensePlanCode, int LicenseDays);
public sealed record RejectMembershipInput(string? Reason);
public sealed record IssueLicenseInput(string PlanCode, int DurationDays);
public sealed record PendingMembershipRequest(Guid RequestId, Guid UserId, string Email, string DisplayName, DateTimeOffset RequestedAtUtc);
public sealed record MembershipDecisionResponse(Guid RequestId, Guid UserId, string RoleName, string LicensePlanCode, DateTimeOffset LicenseExpiresAtUtc);
