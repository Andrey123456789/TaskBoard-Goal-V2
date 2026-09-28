import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { FIELD_LIMITS } from '../../../../shared/field-limits';
import { emptyToNull, notBlank } from '../../../../shared/validators';
import { Project } from '../../../projects/models/project.models';
import { User } from '../../../users/models/user.models';
import { TasksApi } from '../../data-access/tasks-api';
import { TaskDetails, TaskSummary } from '../../models/task.models';

export interface CreateTaskDialogData {
  projects: Project[];
  users: User[];
  tasks: TaskSummary[];
  /** Set when creating a follow-up task from a completed or closed task. */
  sourceTask?: TaskSummary;
}

@Component({
  selector: 'app-create-task-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './create-task-dialog.html',
  styleUrl: './create-task-dialog.scss',
})
export class CreateTaskDialog {
  protected readonly data = inject<CreateTaskDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject<MatDialogRef<CreateTaskDialog, TaskDetails>>(MatDialogRef);
  private readonly tasksApi = inject(TasksApi);

  protected readonly limits = FIELD_LIMITS;
  protected readonly saving = signal(false);

  protected readonly form = new FormGroup({
    title: new FormControl('', {
      nonNullable: true,
      validators: [notBlank, Validators.maxLength(FIELD_LIMITS.taskTitle)],
    }),
    description: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(FIELD_LIMITS.taskDescription)],
    }),
    projectId: new FormControl(this.data.sourceTask?.project.id ?? '', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    assigneeId: new FormControl<string | null>(null),
    relatedTaskIds: new FormControl<string[]>(
      this.data.sourceTask ? [this.data.sourceTask.id] : [],
      {
        nonNullable: true,
      },
    ),
  });

  protected submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.saving.set(true);

    this.tasksApi
      .create({
        title: value.title,
        description: emptyToNull(value.description),
        projectId: value.projectId,
        assigneeId: value.assigneeId,
        relatedTaskIds: value.relatedTaskIds,
      })
      .subscribe({
        next: (task) => this.dialogRef.close(task),
        error: () => this.saving.set(false),
      });
  }
}
