using System.Security.Claims;
using EnterpriseManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManager.Server.Audit;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/audit-events", ListAsync)
            .WithTags("Audit")
            .RequireAuthorization("DirectorOnly");
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        CompanyDbContext db, DateTimeOffset? beforeUtc, int? take, CancellationToken ct)
    {
        var limit = Math.Clamp(take ?? 100, 1, 200);
        var query = db.AuditEvents.AsNoTracking();
        if (beforeUtc.HasValue) query = query.Where(x => x.OccurredAtUtc < beforeUtc.Value);

        var events = await (
            from audit in query
            join user in db.Users on audit.ActorUserId equals user.Id into actorJoin
            from actor in actorJoin.DefaultIfEmpty()
            orderby audit.OccurredAtUtc descending, audit.Id descending
            select new AuditEventListItem(
                audit.Id, audit.ActorUserId, actor == null ? null : actor.Email,
                audit.Action, audit.TargetType, audit.TargetId,
                audit.OccurredAtUtc, audit.IpAddress, audit.Details))
            .Take(limit)
            .ToListAsync(ct);

        return Results.Ok(events);
    }
}

public sealed record AuditEventListItem(
    Guid Id, Guid? ActorUserId, string? ActorEmail, string Action,
    string TargetType, string TargetId, DateTimeOffset OccurredAtUtc,
    string? IpAddress, string? Details);
