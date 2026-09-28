import { HttpErrorResponse } from '@angular/common/http';

/** RFC 9457 problem details as returned by the TaskBoard API. */
export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === 'object' && value !== null && ('title' in value || 'detail' in value);
}

/** Builds a user-facing message from an HTTP error, using the ProblemDetails contract fields. */
export function describeHttpError(error: HttpErrorResponse): string {
  if (error.status === 0) {
    return 'Cannot reach the TaskBoard API. Check that it is running.';
  }

  if (!isProblemDetails(error.error)) {
    return `Request failed (${error.status}).`;
  }

  const problem = error.error;
  const validationMessages = Object.values(problem.errors ?? {}).flat();
  const parts = [problem.title, problem.detail, ...validationMessages].filter(
    (part): part is string => !!part,
  );

  return parts.length > 0 ? parts.join(' ') : `Request failed (${error.status}).`;
}
