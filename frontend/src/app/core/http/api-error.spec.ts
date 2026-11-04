import { HttpErrorResponse } from '@angular/common/http';
import { apiErrorCode } from './api-error';

describe('apiErrorCode', () => {
  it('reads the code of a ProblemDetails answer', () => {
    const error = new HttpErrorResponse({
      status: 422,
      error: { title: 'A business rule prevents this change.', code: 'leave_request.overlapping' },
    });

    expect(apiErrorCode(error)).toBe('leave_request.overlapping');
  });

  it.each([
    ['an answer without code', new HttpErrorResponse({ status: 400, error: { title: 'x' } })],
    ['a body that is not an object', new HttpErrorResponse({ status: 500, error: 'Server error' })],
    ['a network failure', new HttpErrorResponse({ status: 0, error: new ProgressEvent('error') })],
    ['an error that is not from HTTP', new Error('boom')],
  ])('returns null for %s', (_, error) => {
    expect(apiErrorCode(error)).toBeNull();
  });
});
