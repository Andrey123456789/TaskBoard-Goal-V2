import { InjectionToken } from '@angular/core';
import { environment } from '../../../environments/environment';

/** Base URL of the TaskBoard API ('' means the same origin as the application). */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => environment.apiBaseUrl,
});
