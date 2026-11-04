# 0009 – Signal Forms for new forms

- **Status:** Accepted
- **Date:** 2026-11-04

## Context

The request dialog (phase 6a) is the first form with logic between fields: the reason is only
asked for special leave, only sick leave may lie in the past, the end must not be before the start,
and the next step shows a live preview of the request while the employee types. The login page
(phase 5) uses Reactive Forms.

With Reactive Forms, field dependencies are wired imperatively: subscribe to `valueChanges`,
enable or disable controls, reset values, and bridge to signals with `toSignal`. The rest of the
frontend already works with signals (component state, `computed`, `rxResource`).

Angular 22 made **Signal Forms** (`@angular/forms/signals`) stable: the form is built around a
model signal, and a schema declares validation, hidden and disabled state per field.

## Decision

- **New forms use Signal Forms.** The schema states the rules declaratively, e.g. the reason is
  `hidden` unless the type is special leave, and hidden fields do not count for validity.
- Fields are bound with `[formField]`, the form with `[formRoot]`; submission runs through the
  form's `submission.action`, which receives control only when the form is valid. A rejection by
  the backend is returned as a form error (`kind: 'server'`) instead of a separate signal.
- Angular Material controls are bound through the `ControlValueAccessor` interop of `FormField`.
  Their own validators (e.g. `matDatepickerMin`) also appear in the field's errors, but without a
  text; the texts come from the schema's validators, which state each rule explicitly.
- The checks only help while typing; the backend remains the only authority (coding guidelines).
- The login page keeps Reactive Forms for now and is migrated when it is changed anyway.

## Alternatives considered

- **Reactive Forms for everything:** mature and widely known, and one forms API in the project.
  But dependent fields need subscriptions and manual enable/disable, and every value has to be
  bridged to signals for `computed` state such as the live preview.
- **Reactive Forms plus `effect()`** to enable or disable fields from signals: effects are meant for
  side effects outside Angular, not for keeping state in sync; this mixes two models.
- **Template-driven forms:** little code for simple forms, but rules end up in the template and
  are hard to test.

## Consequences

- Two forms APIs coexist until the login page is migrated.
- Material controls rely on the interop layer; native inputs (radio buttons, text areas) bind
  directly.
- The form value is a signal, so derived state (preview request, button labels) is a `computed`.
