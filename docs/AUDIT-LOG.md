# Audit log

The company database stores append-only audit events for security-sensitive administration actions. The Director can read the newest events through `GET /api/v1/audit-events`.

The endpoint accepts optional `beforeUtc` (exclusive cursor) and `take` (1..200, default 100) query parameters. Results are ordered newest first by timestamp and ID. Use the last event timestamp as the next page cursor; if multiple events share the same timestamp, clients should avoid assuming timestamp-only pagination is lossless.

Initial event coverage includes device revocation, session revocation and logout on the current device. The event records the actor, action, target, UTC timestamp, request IP and a short non-secret detail. Passwords, access tokens and refresh tokens must never be written to audit details.

The audit table is part of the EF Core model and requires the initial database migration before deployment. Events are retained until a future retention policy is defined. No endpoint for editing or deleting audit events is exposed.
