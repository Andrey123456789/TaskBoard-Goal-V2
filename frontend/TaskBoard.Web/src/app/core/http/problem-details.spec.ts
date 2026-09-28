import { HttpErrorResponse } from '@angular/common/http';
import { describeHttpError } from './problem-details';

describe('describeHttpError', () => {
  it('uses the ProblemDetails title and detail', () => {
    const error = new HttpErrorResponse({
      status: 409,
      error: {
        title: 'Project has tasks',
        detail: 'A project that has tasks cannot be deleted.',
        status: 409,
      },
    });

    expect(describeHttpError(error)).toBe(
      'Project has tasks A project that has tasks cannot be deleted.',
    );
  });

  it('includes validation messages', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        title: 'One or more validation errors occurred.',
        errors: { Title: ["'Title' must not be empty."] },
      },
    });

    expect(describeHttpError(error)).toBe(
      "One or more validation errors occurred. 'Title' must not be empty.",
    );
  });

  it('reports an unreachable API', () => {
    expect(describeHttpError(new HttpErrorResponse({ status: 0 }))).toContain(
      'Cannot reach the TaskBoard API',
    );
  });

  it('falls back to the status code for non-ProblemDetails bodies', () => {
    expect(describeHttpError(new HttpErrorResponse({ status: 502, error: 'Bad gateway' }))).toBe(
      'Request failed (502).',
    );
  });
});
