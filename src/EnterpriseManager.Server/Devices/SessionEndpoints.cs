using System.Security.Claims;
using EnterpriseManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManager.Server.Devices;

public static class SessionEndpoints
{
    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/sessions").WithTags("Sessions").RequireAuthorization();
        group.MapGet("/me", ListMySessionsAsync);
        group.MapPost("/current/logout", LogoutCurrentDeviceAsync);
        group.MapGet("/", ListAllSessionsAsync).RequireAuthorization("DirectorOnly");
        group.MapPost("/{sessionId:guid}/revoke", RevokeSessionAsync).RequireAuthorization("DirectorOnly");
        return endpoints;
    }

    private static async Task<IResult> ListMySessionsAsync(
        ClaimsPrincipal principal, CompanyDbContext db, CancellationToken ct)
    {
        if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
        var sessions = await (
            from token in db.RefreshTokens
            join device in db.Devices on token.DeviceId equals device.Id
            where token.UserId == userId && token.RevokedAtUtc == null && token.ExpiresAtUtc > DateTimeOffset.UtcNow
            orderby token.CreatedAtUtc descending
            select new SessionListItem(
                token.Id, token.UserId, device.Id, device.DisplayName, device.Platform.ToString(),
                token.CreatedAtUtc, token.ExpiresAtUtc, token.CreatedByIp, device.LastSeenAtUtc))
            .Take(100)
            .ToListAsync(ct);
        return Results.Ok(sessions);
    }

    private static async Task<IResult> ListAllSessionsAsync(CompanyDbContext db, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var sessions = await (
            from token in db.RefreshTokens
            join device in db.Devices on token.DeviceId equals device.Id
            join user in db.Users on token.UserId equals user.Id
            where token.RevokedAtUtc == null && token.ExpiresAtUtc > now
            orderby token.CreatedAtUtc descending
            select new DirectorSessionListItem(
                token.Id, token.UserId, user.Email ?? string.Empty, user.DisplayName,
                device.Id, device.DisplayName, device.Platform.ToString(),
                token.CreatedAtUtc, token.ExpiresAtUtc, token.CreatedByIp, device.LastSeenAtUtc))
            .Take(500)
            .ToListAsync(ct);
        return Results.Ok(sessions);
    }

    private static async Task<IResult> LogoutCurrentDeviceAsync(
        ClaimsPrincipal principal, CompanyDbContext db, HttpContext http, CancellationToken ct)
    {
        if (!TryGetUserId(principal, out var userId) ||
            !Guid.TryParse(principal.FindFirstValue("device_id"), out var deviceId))
            return Results.Unauthorized();

        var activeTokens = await db.RefreshTokens
            .Where(x => x.UserId == userId && x.DeviceId == deviceId && x.RevokedAtUtc == null)
            .ToListAsync(ct);
        if (activeTokens.Count == 0) return Results.NoContent();

        var now = DateTimeOffset.UtcNow;
        var ip = http.Connection.RemoteIpAddress?.ToString();
        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = now;
            token.RevokedByIp = string.IsNullOrWhiteSpace(ip) ? "self-logout" : ip[..Math.Min(ip.Length, 64)];
            token.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        }
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> RevokeSessionAsync(
        Guid sessionId, ClaimsPrincipal principal, CompanyDbContext db,
        HttpContext http, CancellationToken ct)
    {
        if (!TryGetUserId(principal, out _)) return Results.Unauthorized();
        var token = await db.RefreshTokens.SingleOrDefaultAsync(x => x.Id == sessionId, ct);
        if (token is null) return Results.NotFound();
        if (token.RevokedAtUtc is not null) return Results.NoContent();

        token.RevokedAtUtc = DateTimeOffset.UtcNow;
        var ip = http.Connection.RemoteIpAddress?.ToString();
        token.RevokedByIp = string.IsNullOrWhiteSpace(ip) ? "director-session-revocation" : ip[..Math.Min(ip.Length, 64)];
        token.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}

public sealed record SessionListItem(
    Guid SessionId, Guid UserId, Guid DeviceId, string DeviceName, string Platform,
    DateTimeOffset CreatedAtUtc, DateTimeOffset ExpiresAtUtc, string? CreatedByIp,
    DateTimeOffset LastSeenAtUtc);

public sealed record DirectorSessionListItem(
    Guid SessionId, Guid UserId, string UserEmail, string UserDisplayName,
    Guid DeviceId, string DeviceName, string Platform, DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc, string? CreatedByIp, DateTimeOffset LastSeenAtUtc);
