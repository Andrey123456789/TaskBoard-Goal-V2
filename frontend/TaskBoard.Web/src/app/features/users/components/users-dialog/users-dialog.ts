import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Observable, filter, switchMap } from 'rxjs';
import { alreadyNotified } from '../../../../core/http/error-notification.interceptor';
import { Notifier } from '../../../../core/notifications/notifier';
import {
  ConfirmDialog,
  ConfirmDialogData,
} from '../../../../shared/components/confirm-dialog/confirm-dialog';
import { FIELD_LIMITS } from '../../../../shared/field-limits';
import { emailFormat, notBlank } from '../../../../shared/validators';
import { UsersApi } from '../../data-access/users-api';
import { User } from '../../models/user.models';

/** Create, edit, and delete ordinary users without leaving the dashboard. */
@Component({
  selector: 'app-users-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
  ],
  templateUrl: './users-dialog.html',
  styleUrl: './users-dialog.scss',
})
export class UsersDialog {
  private readonly usersApi = inject(UsersApi);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);

  protected readonly limits = FIELD_LIMITS;
  protected readonly users = signal<User[]>([]);
  /** ID of the user being edited, or null when the form creates a new user. */
  protected readonly editingId = signal<string | null>(null);
  protected readonly saving = signal(false);

  protected readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [notBlank, Validators.maxLength(FIELD_LIMITS.userName)],
    }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, emailFormat, Validators.maxLength(FIELD_LIMITS.userEmail)],
    }),
  });

  constructor() {
    this.load();
  }

  protected edit(user: User): void {
    this.editingId.set(user.id);
    this.form.reset({ name: user.name, email: user.email });
  }

  protected cancelEdit(): void {
    this.editingId.set(null);
    this.form.reset();
  }

  protected submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    const request = this.form.getRawValue();
    const editingId = this.editingId();
    const operation: Observable<unknown> = editingId
      ? this.usersApi.update(editingId, request)
      : this.usersApi.create(request);

    this.saving.set(true);
    operation.subscribe({
      next: () => {
        this.saving.set(false);
        this.notifier.success(editingId ? 'User updated' : 'User created');
        this.cancelEdit();
        this.load();
      },
      error: () => this.saving.set(false),
    });
  }

  protected delete(user: User): void {
    this.dialog
      .open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, {
        data: {
          title: 'Delete user',
          message: `Delete ${user.name}? Their tasks are kept and reassigned to "Deleted User".`,
          confirmLabel: 'Delete',
        },
      })
      .afterClosed()
      .pipe(
        filter((confirmed) => confirmed === true),
        switchMap(() => this.usersApi.delete(user.id)),
      )
      .subscribe({
        next: () => {
          this.notifier.success('User deleted');
          if (this.editingId() === user.id) {
            this.cancelEdit();
          }
          this.load();
        },
        error: alreadyNotified,
      });
  }

  private load(): void {
    this.usersApi.list().subscribe({
      next: (users) => this.users.set(users),
      error: alreadyNotified,
    });
  }
}
