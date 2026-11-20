# Membership, approval and licensing

## Company onboarding

A company server is dedicated to one company's central SQLite database. A new worker submits a request to that company's server. The request does not grant access to company data.

## Join request

POST /api/v1/membership/requests

The applicant supplies email, display name and password. The server creates an inactive Identity account and a pending MembershipRequest. Pending accounts cannot authenticate. The endpoint is rate-limited and does not disclose whether an email is already registered.

## Director approval

The Director reviews pending requests at GET /api/v1/membership/requests/pending.

Approval: POST /api/v1/membership/requests/{requestId}/approve

The Director selects one of the configured roles:
- Worker
- Manager
- Finance
- Construction
- Production

The Director also assigns a plan and duration. The current plan codes are STANDARD, PRO and ENTERPRISE; the duration is between 1 and 3650 days. Approval activates the account, assigns its role, creates a company membership and issues the license in one database transaction.

Rejection: POST /api/v1/membership/requests/{requestId}/reject

A rejected account remains inactive and cannot sign in.

## License administration

POST /api/v1/membership/users/{userId}/licenses

Only a Director can issue another license. Current plan codes are placeholders for the future entitlement catalogue; feature-by-feature entitlements are not yet implemented.

The initial Director receives a lifetime OWNER license. Other accounts need a current, active license to authenticate.

## Membership lifecycle

Director-only operations:
- POST /api/v1/membership/users/{userId}/suspend
- POST /api/v1/membership/users/{userId}/reactivate
- POST /api/v1/membership/users/{userId}/revoke

Suspension and revocation deactivate the account and revoke its refresh tokens. Revocation also revokes active licenses. A revoked membership cannot be reactivated.

JWT validation checks the current account, membership and license against the central database on every authenticated request. Current role claims are refreshed from the database, so suspension, revocation, license expiry and role changes take effect without waiting for an access token to expire.

## Not yet implemented

- Fine-grained permission catalogue and custom roles
- Device enrollment, device limits and device revocation
- Session inventory and per-device refresh-token binding
- Email verification, invitations and password recovery
- Reapplication workflow after a rejected request
- Feature entitlement enforcement for plan codes
- Audit log for every Director action
- Database migrations and the schema provisioning utility

Do not deploy this module until database migrations, HTTPS/VPN deployment, secrets management, audit logging and the missing security workflows are completed.
