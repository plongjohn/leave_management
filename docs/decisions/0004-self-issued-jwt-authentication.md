# 0004 – Self-issued JWT with refresh token rotation

- **Status:** Accepted
- **Date:** 2026-10-18

## Context

Employees, managers and HR sign in to the Angular frontend; the API must know who calls it
and which role they have. Bearer tokens in the form of JWTs are the most common approach in
the companies this project is modelled on, so the API authenticates requests with JWTs.

There is no company identity provider in this project, so the API issues the tokens itself.
That makes the token handling security-critical code of our own: it has to follow current
recommendations and be explainable.

## Decision

### Users and passwords

- **ASP.NET Core Identity** via `AddIdentityCore<ApplicationUser>()` with EF Core stores –
  used for user storage, password hashing, lockout and roles only.
- `MapIdentityApi` is **not** used: it issues its own opaque bearer tokens, not JWTs, and does
  not support the token handling described below.
- `ApplicationUser` (login data) and the domain entity `Employee` are separate. An `Employee`
  references its user by id; the domain does not depend on Identity.
- Roles: `Employee`, `Manager`, `HR`.

### Access token

- JWT signed with **HS256**; the signing key (at least 256 bits) comes from configuration –
  user secrets locally, an environment variable or secret store elsewhere – never from Git.
- Lifetime **15 minutes**. Claims: `sub` (user id), `employee_id`, `role`, `jti`; issuer and
  audience are set and validated.
- Returned in the response body of the login and refresh endpoints. The Angular app keeps it
  **in memory only** and sends it as `Authorization: Bearer …`. After a page reload the app
  calls the refresh endpoint to get a new one.

### Refresh token

- Random **256-bit** value, valid for **7 days**.
- Stored in the database only as a **SHA-256 hash**, together with user, expiry, creation
  time, revocation time and the id of the token that replaced it.
- **Rotation:** every refresh revokes the used token and issues a new one.
- **Reuse detection:** if an already revoked refresh token is presented, the whole chain
  (token family) of that login is revoked – a stolen token can be used at most once before
  both the attacker and the user are signed out.
- Sent only as a cookie: `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/auth`.
- Logout revokes the current refresh token and deletes the cookie.

### Endpoints

```text
POST /api/auth/login     email + password → access token (body) + refresh cookie
POST /api/auth/refresh   refresh cookie   → new access token + new refresh cookie
POST /api/auth/logout    refresh cookie   → revokes it, deletes the cookie
```

Failed logins return the same response whether the email exists or not; Identity lockout
limits password guessing.

### Same origin

The Angular dev server proxies `/api` to the backend, and in production the frontend is
served from the same origin. With `SameSite=Strict` and the cookie limited to `/api/auth`,
the browser never sends the refresh token cross-site, so no separate CSRF token is needed.

### Alternatives considered

- **External identity provider** (Keycloak, Entra ID, Auth0) with OpenID Connect: the API
  would only validate tokens, and no security-critical token code would be our own. This is
  the preferred setup when a company identity provider exists; here it would add another
  service to run and configure.
- **Cookie session without JWT** (ASP.NET Core cookie authentication): simpler and well
  suited for a same-origin SPA, but less common in the companies this project is modelled on
  and not usable by non-browser clients.
- **Access and refresh token in `localStorage`:** simple and widespread, but any XSS
  vulnerability could read both tokens; a stolen refresh token would give access for days.
- **RS256 instead of HS256:** asymmetric signing is needed when other services validate the
  tokens without being allowed to issue them. With a single API that both issues and
  validates tokens, a symmetric key is sufficient; switching later only affects token
  creation and validation setup.
- **Long-lived access token without refresh token:** fewer moving parts, but tokens cannot be
  revoked before they expire.

## Consequences

- Roles and permissions can be changed at most 15 minutes before they take effect in issued
  access tokens; revoking a user's refresh tokens ends their session within that time.
- A new database table for refresh tokens and a periodic cleanup of expired entries.
- The Angular app needs an HTTP interceptor that attaches the access token, refreshes once on
  `401` and retries the request, and signs out if the refresh fails.
- The signing key must be configured in every environment; the application refuses to start
  without it.
- Token creation, rotation and reuse detection are covered by integration tests.
