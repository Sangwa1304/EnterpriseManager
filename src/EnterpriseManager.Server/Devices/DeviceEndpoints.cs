using System.Security.Claims;
using EnterpriseManager.Domain.Devices;
using EnterpriseManager.Infrastructure.Persistence;
using EnterpriseManager.Server.Audit;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManager.Server.Devices;

public static class DeviceEndpoints
{
    public static IEndpointRouteBuilder MapDeviceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/devices")
            .WithTags("Devices")
            .RequireAuthorization("DirectorOnly");

        group.MapGet("/", ListDevicesAsync);
        group.MapPost("/{deviceId:guid}/revoke", RevokeDeviceAsync);
        return endpoints;
    }

    private static async Task<IResult> ListDevicesAsync(CompanyDbContext db, CancellationToken ct)
    {
        var devices = await (
            from device in db.Devices
            join user in db.Users on device.UserId equals user.Id
            select new DeviceListItem(
                device.Id, device.DeviceKey, device.UserId, user.Email ?? string.Empty,
                user.DisplayName, device.DisplayName, device.Platform.ToString(),
                device.Status.ToString(), device.RegisteredAtUtc, device.LastSeenAtUtc,
                device.RevokedAtUtc))
            .Take(500)
            .ToListAsync(ct);

        return Results.Ok(devices.OrderByDescending(x => x.LastSeenAtUtc));
    }

    private static async Task<IResult> RevokeDeviceAsync(
        Guid deviceId,
        ClaimsPrincipal principal,
        CompanyDbContext db,
        AuditLogService audit,
        HttpContext http,
        CancellationToken ct)
    {
        var directorIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(directorIdValue, out var directorId))
            return Results.Unauthorized();

        var device = await db.Devices.SingleOrDefaultAsync(x => x.Id == deviceId, ct);
        if (device is null) return Results.NotFound();

        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, ct);

        device.Revoke(directorId);
        var activeTokens = await db.RefreshTokens
            .Where(x => x.DeviceId == deviceId && x.RevokedAtUtc == null)
            .ToListAsync(ct);
        var now = DateTimeOffset.UtcNow;
        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = now;
            token.RevokedByIp = "director-device-revocation";
            token.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        }

        audit.Record(directorId, "device.revoke", "device", deviceId.ToString(),
            http.Connection.RemoteIpAddress?.ToString(),
            $"Revoked device for user {device.UserId}; {activeTokens.Count} refresh-token session(s) invalidated.");
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.NoContent();
    }
}

public sealed record DeviceListItem(
    Guid DeviceId,
    Guid DeviceKey,
    Guid UserId,
    string UserEmail,
    string UserDisplayName,
    string DeviceName,
    string Platform,
    string Status,
    DateTimeOffset RegisteredAtUtc,
    DateTimeOffset LastSeenAtUtc,
    DateTimeOffset? RevokedAtUtc);
