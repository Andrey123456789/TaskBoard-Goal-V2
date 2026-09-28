import { AbstractControl, ValidationErrors } from '@angular/forms';

/** Rejects values that are empty or contain only whitespace (mirrors the API's non-empty rule). */
export function notBlank(control: AbstractControl<string | null>): ValidationErrors | null {
  return (control.value ?? '').trim().length === 0 ? { required: true } : null;
}

/**
 * Mirrors the API's email format rule: exactly one "@" that is neither the first nor the last
 * character. Deliberately not stricter than the API.
 */
export function emailFormat(control: AbstractControl<string | null>): ValidationErrors | null {
  const value = control.value ?? '';
  if (value.length === 0) {
    return null;
  }

  const at = value.indexOf('@');
  const valid = at > 0 && at < value.length - 1 && at === value.lastIndexOf('@');

  return valid ? null : { email: true };
}

/** Converts an empty optional text input to null for the API. */
export function emptyToNull(value: string | null | undefined): string | null {
  return value ? value : null;
}
