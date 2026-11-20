using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EnterpriseManager.Infrastructure.Identity;
using EnterpriseManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EnterpriseManager.Server.Authentication;

public sealed class TokenService(CompanyDbContext db, UserManager<ApplicationUser> users, IOptions<TokenOptions> options)
{
    private readonly TokenOptions _options = options.Value;
    private readonly SigningCredentials _credentials = CreateCredentials(options.Value);

    public async Task<TokenPair> CreateAsync(ApplicationUser user, string? ip, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        db.RefreshTokens.Add(new RefreshTokenRecord
        {
            UserId = user.Id, TokenHash = Hash(raw), CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(_options.RefreshTokenDays), CreatedByIp = NormalizeIp(ip)
        });
        await db.SaveChangesAsync(ct);
        return new TokenPair(await CreateAccessTokenAsync(user, now), _options.AccessTokenMinutes * 60, raw);
    }

    public async Task<TokenPair?> RefreshAsync(string raw, string? ip, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Length > 256) return null;
        var record = await db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == Hash(raw), ct);
        if (record is null) return null;
        var now = DateTimeOffset.UtcNow;
        if (record.RevokedAtUtc is not null || record.ExpiresAtUtc <= now) return null;
        var user = await users.FindByIdAsync(record.UserId.ToString());
        if (user is null || !user.IsActive ||
            !await AuthEndpoints.HasActiveMembershipAndLicenseAsync(db, user.Id, ct)) return null;

        var replacement = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        record.RevokedAtUtc = now;
        record.RevokedByIp = NormalizeIp(ip);
        record.ReplacedByTokenHash = Hash(replacement);
        record.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        db.RefreshTokens.Add(new RefreshTokenRecord
        {
            UserId = user.Id, TokenHash = Hash(replacement), CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(_options.RefreshTokenDays), CreatedByIp = NormalizeIp(ip)
        });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return null; }

        return new TokenPair(await CreateAccessTokenAsync(user, now), _options.AccessTokenMinutes * 60, replacement);
    }

    private async Task<string> CreateAccessTokenAsync(ApplicationUser user, DateTimeOffset now)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
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
