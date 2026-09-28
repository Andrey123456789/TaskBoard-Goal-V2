import { FormControl } from '@angular/forms';
import { emailFormat, emptyToNull, notBlank } from './validators';

describe('shared validators', () => {
  it('notBlank rejects empty and whitespace-only values', () => {
    expect(notBlank(new FormControl(''))).toEqual({ required: true });
    expect(notBlank(new FormControl('   '))).toEqual({ required: true });
    expect(notBlank(new FormControl('Title'))).toBeNull();
  });

  it('emailFormat mirrors the API rule (one "@" not at either end)', () => {
    expect(emailFormat(new FormControl('alice@example.com'))).toBeNull();
    expect(emailFormat(new FormControl('a@b'))).toBeNull();
    expect(emailFormat(new FormControl(''))).toBeNull();
    expect(emailFormat(new FormControl('not-an-email'))).toEqual({ email: true });
    expect(emailFormat(new FormControl('@example.com'))).toEqual({ email: true });
    expect(emailFormat(new FormControl('alice@'))).toEqual({ email: true });
    expect(emailFormat(new FormControl('a@b@c'))).toEqual({ email: true });
  });

  it('emptyToNull converts empty optional text to null', () => {
    expect(emptyToNull('')).toBeNull();
    expect(emptyToNull(undefined)).toBeNull();
    expect(emptyToNull('text')).toBe('text');
  });
});
