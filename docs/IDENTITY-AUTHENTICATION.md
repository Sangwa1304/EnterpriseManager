# Identity and authentication

## Included in this tranche

- ASP.NET Core Identity stores password hashes and account security state.
- The first Director is created only through a one-time setup endpoint.
- Public self-registration is not exposed.
- Login is protected by an endpoint rate-limit policy and Identity lockout.
- Access tokens are short-lived signed JWTs.
- Refresh tokens are random opaque values; only SHA-256 hashes are stored.
- Refresh tokens rotate on use and have an optimistic concurrency stamp.
- Each server instance remains dedicated to one company's database.

## Initial company setup

The server operator must provision the company database schema before setup. The setup endpoint is disabled unless Bootstrap:SetupKey is configured as a high-entropy secret outside source control (for example an environment variable). Call it once from the trusted local network. After setup, remove the setup key from the server environment and restart the server. The endpoint refuses setup if a company or identity already exists.

Endpoint: POST /api/v1/auth/setup/director

Header: X-Enterprise-Setup-Key

Body fields: LegalName, BusinessIdentifier, TimeZone, DirectorEmail, DirectorName, DirectorPassword.

The initial Director role is assigned server-side. Clients cannot select or assign their own roles.

## Authentication endpoints

- POST /api/v1/auth/login (requires device key, device name and platform)
- POST /api/v1/auth/refresh
- GET /api/v1/auth/me (Bearer access token)

Never log credentials, access tokens, refresh tokens or setup keys. Require HTTPS or a private trusted VPN. Configure a unique signing key of at least 32 random bytes, issuer and audience using environment variables or a secret manager.

## Operational requirements not yet completed

- Database migrations and the provisioning utility must be completed before deploying this API.
- Configure Authentication:Tokens:SigningKey, Issuer and Audience outside appsettings and source control.
- Device registration, membership requests, Director approval, fine-grained permissions and administrative revocation remain separate work.
- Implement account email verification and password recovery before production release.
- Configure backup and protected retention for the company's central SQLite database.
