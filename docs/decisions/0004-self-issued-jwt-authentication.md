# 0004 – Self-issued JWT with refresh token rotation

- **Status:** Accepted
- **Date:** 2026-10-18
- **Updated:** 2026-10-20 – roles belong to the employee, link direction user → employee,
  details settled during implementation (phase 5)

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
  used for user storage, password hashing and lockout only. `SignInManager` is added for
  `CheckPasswordSignInAsync` (password check with lockout); its cookie sign-in is not used.
- `MapIdentityApi` is **not** used: it issues its own opaque bearer tokens, not JWTs, and does
  not support the token handling described below.
- **Login data and employee are separate.** `ApplicationUser` (Infrastructure) holds the login:
  user name, password hash, lockout. It references the domain entity `Employee` through a
  required, unique `EmployeeId` – one login per employee. `Employee` knows nothing about
  logins; it is a list entry of the company, not an account.
- The user name and email of a login are the employee's email at the time the login is
  created. Keeping them in sync when an employee's email changes belongs to the admin
  feature.
- Password policy after NIST SP 800-63B: at least 12 characters, no composition rules.
- Lockout after 5 failed attempts for 15 minutes. In addition, a rate limiter allows 10 login
  requests per minute and client IP address, which also slows down trying one password on
  many accounts.

### Roles

- Roles are **`Employee`, `Manager` and `Hr`**, stored as `Role` on the domain entity `Employee`.
  Identity's role tables are not used: the database context derives from
  `IdentityUserContext<ApplicationUser, Guid>` instead of `IdentityDbContext`, which has no
  role tables.
- Reason: the role is a business concept. The domain rule "HR decides on requests of
  employees without a manager" needs it, and the domain must not depend on Identity.
  Keeping the role in one place avoids two sources that could disagree.
- The `role` claim of the access token is read from the employee whenever a token is issued.
- Authorization: a **fallback policy** requires a signed-in user on every endpoint (deny by
  default); only login, refresh, logout, `/health` and – in Development – the OpenAPI document
  and Scalar allow anonymous access. Named policies `ManagerOrHr` (approvals) and `Hr`
  (entitlements, holidays, administration) restrict endpoints by role. Rules that depend on
  data, such as "only the employee's own manager decides", stay in the domain.

### Access token

- JWT signed with **HS256**. The signing key is a base64-encoded random value of at least
  256 bits. It comes from the environment variable `Jwt__SigningKey` (ADR 0005) –
  never from Git. The API refuses to start without a valid key.
- Lifetime **15 minutes**, validated with a clock skew of 30 seconds instead of the default
  five minutes. Claims: `sub` (user id), `employee_id`, `role`, `jti`; issuer and audience are
  set and validated. Inbound claims are not mapped to the long .NET claim types.
- Returned in the response body of the login and refresh endpoints in the shape of an OAuth 2.0
  token response (`accessToken`, `tokenType`, `expiresIn`). The Angular app keeps it **in
  memory only** and sends it as `Authorization: Bearer …`. After a page reload the app calls
  the refresh endpoint to get a new one.
- `GET /api/me` returns the signed-in employee (id, name, email, role), so the frontend does
  not have to decode the token.

### Refresh token

- Random **256-bit** value, valid for **7 days**. Every refresh issues a new token with the full
  lifetime, so a session ends after seven days without activity.
- Stored in the database only as a **SHA-256 hash**, together with user, token family, expiry,
  creation time, revocation time and the id of the token that replaced it.
- **Rotation:** every refresh revokes the used token and issues a new one of the same family.
- **Reuse detection:** if an already revoked refresh token is presented, the whole family of
  that login is revoked – a stolen token can be used at most once before both the attacker and
  the user are signed out. Other sessions of the same user are not affected.
- Two parallel refreshes with the same token: a concurrency token (PostgreSQL `xmin`) lets only
  one of them succeed; the other gets `401` without revoking the family.
- Sent only as the cookie `refresh_token`: `HttpOnly`, `Secure`, `SameSite=Strict`,
  `Path=/api/auth`.
- **"Stay signed in"** (`rememberMe` at login): with it, the cookie expires with the token;
  without it, it is a session cookie that the browser drops when it is closed. The choice is
  stored with the token and kept on every rotation. The token expires on the server after its
  lifetime either way.
- Logout revokes the current refresh token and deletes the cookie.
- A background service deletes expired refresh tokens every six hours.

### Endpoints

```text
POST /api/auth/login     email, password, rememberMe → access token (body) + refresh cookie
POST /api/auth/refresh   refresh cookie              → new access token + new refresh cookie
POST /api/auth/logout    refresh cookie              → revokes it, deletes the cookie
GET  /api/me             access token                → the signed-in employee
```

Failed logins return the same response (`401`, code `auth.login_failed`) whether the email
exists, the password is wrong or the account is locked. For unknown emails a dummy password
hash is verified, so the response time does not reveal which emails exist. A failed refresh
returns `401` with code `auth.refresh_failed` and deletes the cookie.

### Same origin

The Angular dev server proxies `/api` to the backend, and in production the frontend is
served from the same origin. With `SameSite=Strict` and the cookie limited to `/api/auth`,
the browser never sends the refresh token cross-site, so no separate CSRF token is needed.
Browsers accept `Secure` cookies from `http://localhost`, so local development works without
HTTPS.

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
- **Roles in Identity's role tables** (`IdentityDbContext`, `AddRoles`): the usual Identity
  setup, but the domain would then need the role passed in from outside, and the role would
  exist twice once the employee needs it too.
- **`Employee` references its login (`Employee.UserId`):** the domain entity would carry an
  account concept it never uses; the login is what needs to know its employee.
- **Separate DbContext for Identity:** keeps the Identity tables apart, but the foreign key to
  `employees` would cross contexts and the migrations would be split.
- **Grace period for reused refresh tokens** (some providers accept the previous token for a
  few seconds): avoids signing out a user when two browser tabs refresh at almost the same
  time, but weakens reuse detection. Here the frontend makes sure only one refresh runs at a
  time instead – also across tabs, e.g. with the Web Locks API.

## Consequences

- Roles and permissions can be changed at most 15 minutes before they take effect in issued
  access tokens; revoking a user's refresh tokens ends their session within that time.
- A table for refresh tokens and a periodic cleanup of expired entries.
- The Angular app needs an HTTP interceptor that attaches the access token, refreshes once on
  `401` and retries the request, and signs out if the refresh fails. Parallel requests and open
  tabs have to share a single refresh, otherwise reuse detection would sign the user out.
- The signing key must be configured in every environment; the application refuses to start
  without it.
- Token creation, rotation and reuse detection are covered by integration tests.
