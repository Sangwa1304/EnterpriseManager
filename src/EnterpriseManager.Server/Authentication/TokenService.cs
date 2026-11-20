using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EnterpriseManager.Infrastructure.Identity;
using EnterpriseManager.Domain.Devices;
using EnterpriseManager.Infrastructure.Persistence;
using EnterpriseManager.Server.Audit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EnterpriseManager.Server.Authentication;

public sealed class TokenService(CompanyDbContext db, UserManager<ApplicationUser> users, IOptions<TokenOptions> options, AuditLogService audit)
{
    private readonly TokenOptions _options = options.Value;
    private readonly SigningCredentials _credentials = CreateCredentials(options.Value);

    public async Task<TokenPair> CreateAsync(ApplicationUser user, Guid deviceId, string? ip, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var session = new RefreshTokenRecord
        {
            UserId = user.Id, DeviceId = deviceId, TokenHash = Hash(raw), CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(_options.RefreshTokenDays), CreatedByIp = NormalizeIp(ip)
        };
        db.RefreshTokens.Add(session);
        await db.SaveChangesAsync(ct);
        return new TokenPair(await CreateAccessTokenAsync(user, deviceId, session.Id, now), _options.AccessTokenMinutes * 60, raw);
    }

    public async Task<TokenPair?> RefreshAsync(string raw, string? ip, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Length > 256) return null;
        var record = await db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == Hash(raw), ct);
        if (record is null) return null;
        var now = DateTimeOffset.UtcNow;
        if (record.RevokedAtUtc is not null)
        {
            if (record.ReplacedByTokenHash is not null)
                await RevokeDeviceSessionsAfterReuseAsync(record.UserId, record.DeviceId, ip, ct);
            return null;
        }
        if (record.ExpiresAtUtc <= now) return null;
        var user = await users.FindByIdAsync(record.UserId.ToString());
        var device = await db.Devices.SingleOrDefaultAsync(x => x.Id == record.DeviceId, ct);
        if (user is null || !user.IsActive || device is null || device.Status != DeviceStatus.Active ||
            !await AuthEndpoints.HasActiveMembershipAndLicenseAsync(db, user.Id, ct)) return null;
        device.Touch(device.DisplayName);

        var replacement = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        record.RevokedAtUtc = now;
        record.RevokedByIp = NormalizeIp(ip);
        record.ReplacedByTokenHash = Hash(replacement);
        record.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        var replacementSession = new RefreshTokenRecord
        {
            UserId = user.Id, DeviceId = device.Id, TokenHash = Hash(replacement), CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(_options.RefreshTokenDays), CreatedByIp = NormalizeIp(ip)
        };
        db.RefreshTokens.Add(replacementSession);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            var latest = await db.RefreshTokens.SingleOrDefaultAsync(x => x.Id == record.Id, ct);
            if (latest?.RevokedAtUtc is not null && latest.ReplacedByTokenHash is not null)
                await RevokeDeviceSessionsAfterReuseAsync(latest.UserId, latest.DeviceId, ip, ct);
            return null;
        }

        return new TokenPair(await CreateAccessTokenAsync(user, device.Id, replacementSession.Id, now), _options.AccessTokenMinutes * 60, replacement);
    }

    private async Task RevokeDeviceSessionsAfterReuseAsync(
        Guid userId, Guid deviceId, string? ip, CancellationToken ct)
    {
        var activeSessions = await db.RefreshTokens
            .Where(x => x.UserId == userId && x.DeviceId == deviceId && x.RevokedAtUtc == null)
            .ToListAsync(ct);
        var now = DateTimeOffset.UtcNow;
        foreach (var session in activeSessions)
        {
            session.RevokedAtUtc = now;
            session.RevokedByIp = NormalizeIp(ip) ?? "refresh-token-reuse";
            session.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        }

        audit.Record(null, "security.refresh-token-reuse", "device", deviceId.ToString(),
            ip, $"Detected reuse of a rotated refresh token. Revoked {activeSessions.Count} active session(s) for user {userId} on this device.");
        await db.SaveChangesAsync(ct);
    }

    private async Task<string> CreateAccessTokenAsync(ApplicationUser user, Guid deviceId, Guid sessionId, DateTimeOffset now)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("device_id", deviceId.ToString()),
            new("session_id", sessionId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        claims.AddRange((await users.GetRolesAsync(user)).Select(x => new Claim(ClaimTypes.Role, x)));
        var expires = now.AddMinutes(_options.AccessTokenMinutes);
        var token = new JwtSecurityToken(_options.Issuer, _options.Audience, claims,
            now.UtcDateTime, expires.UtcDateTime, _credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static SigningCredentials CreateCredentials(TokenOptions o)
    {
        if (string.IsNullOrWhiteSpace(o.SigningKey) || Encoding.UTF8.GetByteCount(o.SigningKey) < 32)
            throw new InvalidOperationException("Authentication:Tokens:SigningKey must contain at least 32 UTF-8 bytes.");
        if (string.IsNullOrWhiteSpace(o.Issuer) || string.IsNullOrWhiteSpace(o.Audience))
            throw new InvalidOperationException("JWT issuer and audience must be configured.");
        if (o.AccessTokenMinutes is < 5 or > 60 || o.RefreshTokenDays is < 1 or > 30)
            throw new InvalidOperationException("Token lifetimes are outside allowed limits.");
        return new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.SigningKey)), SecurityAlgorithms.HmacSha256);
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static string? NormalizeIp(string? ip) => string.IsNullOrWhiteSpace(ip) ? null : ip[..Math.Min(ip.Length, 64)];
}
public sealed record TokenPair(string AccessToken, int ExpiresIn, string RefreshToken);
