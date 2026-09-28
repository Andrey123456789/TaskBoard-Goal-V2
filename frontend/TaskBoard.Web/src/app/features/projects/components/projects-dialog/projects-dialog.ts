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
import { emptyToNull, notBlank } from '../../../../shared/validators';
import { ProjectsApi } from '../../data-access/projects-api';
import { Project } from '../../models/project.models';

/** Create, edit, and delete projects without leaving the dashboard. */
@Component({
  selector: 'app-projects-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
  ],
  templateUrl: './projects-dialog.html',
  styleUrl: './projects-dialog.scss',
})
export class ProjectsDialog {
  private readonly projectsApi = inject(ProjectsApi);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);

  protected readonly limits = FIELD_LIMITS;
  protected readonly projects = signal<Project[]>([]);
  /** ID of the project being edited, or null when the form creates a new project. */
  protected readonly editingId = signal<string | null>(null);
  protected readonly saving = signal(false);

  protected readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [notBlank, Validators.maxLength(FIELD_LIMITS.projectName)],
    }),
    description: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(FIELD_LIMITS.projectDescription)],
    }),
  });

  constructor() {
    this.load();
  }

  protected edit(project: Project): void {
    this.editingId.set(project.id);
    this.form.reset({ name: project.name, description: project.description ?? '' });
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

    const value = this.form.getRawValue();
    const request = { name: value.name, description: emptyToNull(value.description) };
    const editingId = this.editingId();
    const operation: Observable<unknown> = editingId
      ? this.projectsApi.update(editingId, request)
      : this.projectsApi.create(request);

    this.saving.set(true);
    operation.subscribe({
      next: () => {
        this.saving.set(false);
        this.notifier.success(editingId ? 'Project updated' : 'Project created');
        this.cancelEdit();
        this.load();
      },
      error: () => this.saving.set(false),
    });
  }

  protected delete(project: Project): void {
    this.dialog
      .open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, {
        data: {
          title: 'Delete project',
          message: `Delete ${project.name}? Only projects without tasks can be deleted.`,
          confirmLabel: 'Delete',
        },
      })
      .afterClosed()
      .pipe(
        filter((confirmed) => confirmed === true),
        switchMap(() => this.projectsApi.delete(project.id)),
      )
      .subscribe({
        next: () => {
          this.notifier.success('Project deleted');
          if (this.editingId() === project.id) {
            this.cancelEdit();
          }
          this.load();
        },
        error: alreadyNotified,
      });
  }

  private load(): void {
    this.projectsApi.list().subscribe({
      next: (projects) => this.projects.set(projects),
      error: alreadyNotified,
    });
  }
}
