# 0008 – API conventions for business rules, concurrency and files

- **Status:** Accepted
- **Date:** 2026-11-03

## Context

Phase 6 adds the first endpoints that change data: employees submit and cancel requests and report
sick leave with a medical certificate. Several questions come up for every such endpoint: how a
violated business rule reaches the client, what happens when two people change the same request,
how the request dialog can show the effect of a request before it is submitted, how employees are
kept away from other employees' data, and where uploaded files are stored. Answering them once keeps
the endpoints consistent.

## Decision

### Errors

- **Input shape** (missing fields, end before start, file too large) is checked with FluentValidation
  and answered with **400** `ValidationProblemDetails` listing the fields.
- **Violated business rules** throw a `BusinessRuleViolationException` in the domain (ADR 0007).
  One `IExceptionHandler` (`BusinessRuleExceptionHandler`) turns it into **422** ProblemDetails with
  the stable `code`, e.g. `leave_request.overlapping`. Endpoints do not catch these exceptions; the
  frontend maps the code to a German text.
- **Concurrent changes** (`DbUpdateConcurrencyException`) become **409** with
  `code: concurrency_conflict`.
- Expected rejections are logged as information with the code only, never with personal data.

### Optimistic concurrency with the version the client saw

Requests that can change after the client loaded them (cancel, approve, reject, revoke) carry the
**`version`** from the response (PostgreSQL `xmin`). The application sets it as the original value of
the concurrency token before saving, so a decision made in the meantime makes the save fail with 409
instead of silently cancelling something the user has not seen.

Submissions are different: they create a new row, so there is no version to compare. Two submissions
of the same employee at the same time could both pass the overlap and balance checks; the submission
therefore runs in a transaction that **locks the employee's row** (`SELECT … FOR UPDATE`). Only the
submissions of one employee wait for each other.

### Preview instead of rules in the frontend

`POST /api/me/leave-requests/preview` runs the real submission rules without saving and returns the
working days, holidays in the period, the balance afterwards (or the returned vacation days for sick
leave), the violated rule as `errorCode`, the approver and the absent team members. A violated rule
is an expected result here, so the answer is 200. The frontend calls it while the employee types; the
rules exist only in the backend (coding guidelines).

### Own data under `/api/me`, foreign data as 404

- Endpoints that act on the signed-in employee live under **`/api/me/…`** and take the employee id
  from the access token, never from the URL.
- Where an id points to a resource the user may not see (someone else's request, a certificate for a
  manager), the answer is **404**, not 403, so the existence of the resource is not revealed.

### Sick leave and certificates

- Sick leave is reported with `POST /api/me/sick-leaves` as **multipart form data**: the report and
  the optional certificate arrive together and are saved in one transaction. The JSON endpoint for
  other request types rejects sick leave.
- The file type is **recognised by the first bytes** (PDF, PNG, JPEG); the client's file name and
  content type are not trusted, and any path in the file name is dropped. At most 5 MB.
- Certificates are **stored in PostgreSQL** (`bytea`, table `medical_certificates`), one per sick
  leave. They are small and rare, must stay consistent with their request, and the database backup
  covers them. A file store would add a second system to operate and to keep in sync.
- Certificates are health data (GDPR Art. 9): only the employee and HR can download them, never the
  manager. Downloads are sent as attachment with `Cache-Control: no-store` and
  `X-Content-Type-Options: nosniff`.
- Antiforgery is disabled for the upload: the API authenticates with a bearer token in the
  `Authorization` header (ADR 0004), which a foreign site cannot send.

## Alternatives considered

- **403 for foreign resources:** more explicit, but reveals that the resource exists.
- **Business rules as 409:** some APIs use 409 for every conflict; 422 separates "the data does not
  fit the rules" from "someone else changed the data".
- **`If-Match` header with an ETag** instead of `version` in the body: the HTTP standard way, but
  clumsier for a single-page app that already holds the version in its model. Possible later.
- **Two-step upload** (upload first, then submit with the file id): common for large files, but needs
  clean-up of orphaned uploads.
- **Blob storage or file system** for certificates: better for many or large files; not worth the
  extra infrastructure for a few small files per year.

## Consequences

- Every changing endpoint answers 400, 422 or 409 in the same way; the frontend needs one error
  mapping.
- The preview costs a request per change in the dialog; the frontend debounces it.
- Database backups contain health data and must be protected like the database itself.
