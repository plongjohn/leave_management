import { HttpErrorResponse } from '@angular/common/http';

/** `code` of a 409: someone changed the data since it was loaded (ADR 0008). */
export const CONCURRENCY_CONFLICT = 'concurrency_conflict';

/**
 * The stable `code` of a ProblemDetails answer (422 business rule, 409 concurrency – ADR 0008),
 * or `null` for any other failure (network, 500, validation without code).
 */
export function apiErrorCode(error: unknown): string | null {
  if (!(error instanceof HttpErrorResponse)) {
    return null;
  }

  const body: unknown = error.error;
  if (
    typeof body === 'object' &&
    body !== null &&
    'code' in body &&
    typeof body.code === 'string'
  ) {
    return body.code;
  }

  return null;
}
