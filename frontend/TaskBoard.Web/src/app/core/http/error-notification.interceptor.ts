import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { Notifier } from '../notifications/notifier';
import { describeHttpError } from './problem-details';

/**
 * Error callback for API subscriptions that need no recovery: the interceptor has already shown
 * the failure, so the error must not also surface as an unhandled application error.
 */
export const alreadyNotified = (): void => undefined;

/** Shows every failed API call to the user; callers still receive the error. */
export const errorNotificationInterceptor: HttpInterceptorFn = (request, next) => {
  const notifier = inject(Notifier);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        notifier.error(describeHttpError(error));
      }

      return throwError(() => error);
    }),
  );
};
