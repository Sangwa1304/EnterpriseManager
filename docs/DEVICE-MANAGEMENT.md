# Device management

## Device identity

The native client generates a random installation identifier (GUID) once and sends it with login. The server binds the authenticated account to a registered device record. The identifier is an installation identifier, not hardware attestation; a determined user can reinstall or generate another identifier. The configured device limit is therefore an administrative control, not a cryptographic anti-cloning guarantee.

Supported platform labels: Windows, Android and Other.

## Login and device registration

POST /api/v1/auth/login requires:
- Email
- Password
- DeviceKey (client-generated GUID)
- DeviceName (maximum 100 characters)
- Platform (Windows, Android or Other)

The server checks credentials, account state, membership and license before registering a new device. The default limit is five active devices per user. Configure Devices:MaxPerUser (bounded to 1..20) on the server.

A revoked device cannot log in again with its old DeviceKey. The user must use a new installation identifier, and the Director can review the new registration.

## Token binding

Every refresh token is linked to a registered device. Access tokens carry the server-side device record ID. Every authenticated request checks that the user, membership, license and device are still active. This makes device revocation effective for existing access tokens, not only future logins. Refresh-token rotation preserves the device binding.

## Director controls

- GET /api/v1/devices lists up to 500 devices and their account association.
- POST /api/v1/devices/{deviceId}/revoke permanently revokes the device and all its active refresh tokens.

## Session controls

- GET `/api/v1/sessions/me` lists the signed-in user's active refresh-token sessions and their device metadata.
- POST `/api/v1/sessions/current/logout` revokes refresh tokens for the current account on the current device.
- GET `/api/v1/sessions` (Director only) lists active sessions across the company.
- POST `/api/v1/sessions/{sessionId}/revoke` (Director only) revokes one refresh-token session.

A session revocation invalidates its refresh token immediately. An already-issued access token can remain usable until its short expiry (at most the configured 60 minutes), unless the entire device is revoked, which is checked on every authenticated request. Immediate per-session access-token invalidation requires binding access tokens to a server-side session identifier and checking that identifier on every request; that is a separate hardening step.

## Limitations before production

- The client must securely persist its random installation identifier using platform secure storage.
- No hardware-backed device attestation is implemented.
- No user-facing device inventory or session history UI is implemented.
- A refresh-token session inventory and explicit per-session logout are still outstanding.
- Device audit events and alerts for unusual enrollment are still outstanding.
- Database migrations must add the Devices table and RefreshTokens.DeviceId before deployment.
