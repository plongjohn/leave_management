# 0007 – Leave entitlement, working days and balance rules

- **Status:** Accepted
- **Date:** 2026-10-18

## Context

The application manages leave for a small company in Austria. The core questions are how leave
is counted, how unused leave is carried over and when it expires, and which kinds of absence
use the entitlement. The relevant law is the Austrian Urlaubsgesetz (UrlG); details such as
special leave are set by the collective agreement (Kollektivvertrag) and differ between
industries. The domain has to stay testable without a database (coding guidelines).

## Decision

### Counting leave

- **The entitlement is kept in working days: 25 per year** (`LeaveEntitlement.StandardDays`).
  This is equal to the statutory 30 workdays (Monday to Saturday, § 2 UrlG) for a five-day
  week, and it is what most Austrian HR software shows.
- **HR can raise or lower the entitlement** per employee and year, e.g. for the sixth week after
  25 years of service or extra days from the employment contract. Days that are already approved
  or pending must stay covered.
- **Working days** are Monday to Friday minus holidays. A `Holiday` is one of:
  `Statutory` (the 13 Austrian public holidays, computed by `AustrianPublicHolidays`),
  `CompanyDayOff`, or `CompanyHalfDay` (counts as half a working day, by default 24 and 31 December).
  Employees cannot request half days themselves; days are `decimal` because of the company half days.
- **Every request stores its working days as a snapshot** (`leave_request_days`, one row per
  date). Later changes to the holiday calendar do not change existing requests, and the team
  calendar can query absences per day.
- **Holidays are reference data seeded by a migration** for 2025–2035 (`HasData`, generated from
  `AustrianPublicHolidays`). Extending the range needs a new migration; maintenance by HR follows
  in phase 6.

### Carry-over and expiry

- **Leave year = calendar year.** Every year's entitlement is a separate pot that can be used
  from 1 January of that year until **31 December of the second following year**
  (§ 4 Abs 5 UrlG). There is no leave in advance from a future year.
- **Days are taken from the oldest pot first.** `LeaveBalanceCalculator` walks through all days
  of pending and approved vacation in date order and takes each day from the oldest pot that is
  still valid on that day. The carry-over is therefore calculated, never stored, and requests
  across the turn of the year work naturally.
- On every new request the whole allocation is recalculated, because an earlier new request can
  take days that a later, already planned request relied on.
- **Pending requests reserve their days**; rejected and cancelled requests release them because
  only pending and approved requests are counted.

### Types of absence

| Type | Approval | Uses entitlement | Limit |
|---|---|---|---|
| Vacation | manager | yes | balance |
| Time off in lieu | manager | no (no time account yet) | – |
| Sick leave | none, approved when reported; may lie in the past | no | – |
| Special leave | manager | no | per occasion (`SpecialLeavePolicy`) |

Special leave (Dienstverhinderung, § 8 Abs 3 AngG) needs a reason. The maximum days per occasion
are typical collective agreement values and kept in code for the MVP:

| Reason | Working days |
|---|---|
| Own wedding | 3 |
| Birth of a child | 1 |
| Death of spouse/partner, child or parent | 3 |
| Death of sibling, parent-in-law or grandparent | 1 |
| Wedding of a child or sibling | 1 |
| Relocation | 2 |
| Care leave (Pflegefreistellung, § 16 UrlG) | 5 per working year |

The working year for care leave starts on the anniversary of the entry date.

### Shape of the domain code

- Rules that need other data (overlap, balance, limits) are checked in `LeaveRequest.Submit`;
  the caller passes everything in a `SubmissionContext`. The domain stays free of data access and
  is tested without mocks.
- Violations throw a `BusinessRuleViolationException` with a stable code
  (e.g. `leave_request.insufficient_balance`); the API will map it to a ProblemDetails response
  and the frontend maps the code to a German text.
- "Today" is passed in as a date in the company's time zone; the application layer derives it
  from `TimeProvider`.

### Alternatives considered

- **Entitlement in workdays (30):** legally the basis, but harder to understand for employees
  with a five-day week and needs a conversion everywhere.
- **Stored carry-over** (`CarriedOverDays` entered by HR at the end of the year): simpler, but
  expiry is not enforced, and wrong manual entries are easy to make.
- **Requests limited to one calendar year:** keeps one request in one leave year, but employees
  would have to split their Christmas holidays into two requests.
- **Special leave limits in a table maintained by HR:** closer to reality because they depend
  on the collective agreement, but more effort; possible later.

## Known gaps

- **Sick leave during approved vacation** (§ 5 UrlG: more than three calendar days do not count
  as leave) – planned for phase 6 as an HR action that returns the vacation days. Until then the
  overlap rule applies to all types. A skipped test marks the gap.
- **Requests of employees without a manager** cannot be decided yet; HR takes over once roles
  exist (phase 5). A skipped test marks the gap.
- Part-time and other working-week models, pro-rata entitlements in the entry year and
  automatic sixth week are not supported; HR adjusts the entitlement manually.

## Consequences

- The balance is always derived from entitlements and requests; there is no stored balance that
  can drift.
- Loading the data for a submission means all entitlements and active requests of one employee –
  a small, indexed query.
- Changing the expiry rule or the order of consumption affects only `LeaveBalanceCalculator`.
