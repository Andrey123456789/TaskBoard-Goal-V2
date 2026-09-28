import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  EMPTY,
  Observable,
  Subject,
  catchError,
  filter,
  forkJoin,
  map,
  merge,
  switchMap,
} from 'rxjs';
import { alreadyNotified } from '../../../../core/http/error-notification.interceptor';
import { Notifier } from '../../../../core/notifications/notifier';
import {
  ConfirmDialog,
  ConfirmDialogData,
} from '../../../../shared/components/confirm-dialog/confirm-dialog';
import { FIELD_LIMITS } from '../../../../shared/field-limits';
import { emptyToNull, notBlank } from '../../../../shared/validators';
import { User } from '../../../users/models/user.models';
import { TasksApi } from '../../data-access/tasks-api';
import { TaskDetails, TaskSummary } from '../../models/task.models';
import {
  STATUS_LABELS,
  canCreateFollowUp,
  canStart,
  isCoreEditable,
  isDeletable,
  nextStatusAction,
} from '../../models/task-workflow';

@Component({
  selector: 'app-task-detail-panel',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    MatTooltipModule,
  ],
  templateUrl: './task-detail-panel.html',
  styleUrl: './task-detail-panel.scss',
})
export class TaskDetailPanel {
  readonly taskId = input.required<string>();
  /** Active ordinary users that can be assigned. */
  readonly users = input.required<User[]>();
  /** Changing this value reloads the task (for changes made outside the panel). */
  readonly refreshKey = input(0);

  readonly closed = output<void>();
  /** Emitted after a change that affects the board (core fields, status, deletion). */
  readonly changed = output<void>();
  readonly taskSelected = output<string>();
  readonly createRelatedRequested = output<TaskDetails>();

  private readonly tasksApi = inject(TasksApi);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);
  private readonly reload$ = new Subject<void>();

  protected readonly limits = FIELD_LIMITS;
  protected readonly statusLabels = STATUS_LABELS;
  protected readonly task = signal<TaskDetails | null>(null);
  protected readonly allTasks = signal<TaskSummary[]>([]);
  protected readonly busy = signal(false);

  protected readonly editable = computed(() => this.isTask(isCoreEditable));
  protected readonly deletable = computed(() => this.isTask(isDeletable));
  protected readonly followUpAllowed = computed(() => this.isTask(canCreateFollowUp));
  protected readonly action = computed(() => {
    const task = this.task();
    return task ? nextStatusAction(task.status) : null;
  });
  protected readonly actionEnabled = computed(() => {
    const task = this.task();
    return !!task && canStart(task);
  });
  protected readonly relatedCandidates = computed(() => {
    const task = this.task();
    const relatedIds = new Set(task?.relatedTasks.map((related) => related.id));
    return this.allTasks().filter((other) => other.id !== task?.id && !relatedIds.has(other.id));
  });

  protected readonly form = new FormGroup({
    title: new FormControl('', {
      nonNullable: true,
      validators: [notBlank, Validators.maxLength(FIELD_LIMITS.taskTitle)],
    }),
    description: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(FIELD_LIMITS.taskDescription)],
    }),
    assigneeId: new FormControl<string | null>(null),
  });

  constructor() {
    const externalTrigger = computed(() => ({ id: this.taskId(), refreshKey: this.refreshKey() }));

    merge(
      toObservable(externalTrigger).pipe(map(({ id }) => id)),
      this.reload$.pipe(map(() => this.taskId())),
    )
      .pipe(
        switchMap((id) =>
          forkJoin({ task: this.tasksApi.get(id), allTasks: this.tasksApi.list() }).pipe(
            catchError((error: unknown) => {
              if (error instanceof HttpErrorResponse && error.status === 404) {
                this.closed.emit();
              }
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe(({ task, allTasks }) => {
        this.showTask(task);
        this.allTasks.set(allTasks);
      });
  }

  protected save(): void {
    const task = this.task();
    if (!task || this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.run(
      this.tasksApi.update(task.id, {
        title: value.title,
        description: emptyToNull(value.description),
        assigneeId: value.assigneeId,
      }),
      'Task saved',
      true,
    );
  }

  protected advance(): void {
    const task = this.task();
    const action = this.action();
    if (task && action) {
      this.run(
        this.tasksApi.changeStatus(task.id, action.target),
        `Task moved to ${STATUS_LABELS[action.target]}`,
        true,
      );
    }
  }

  protected delete(): void {
    const task = this.task();
    if (!task) {
      return;
    }

    this.dialog
      .open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, {
        data: { title: 'Delete task', message: `Delete "${task.title}"?`, confirmLabel: 'Delete' },
      })
      .afterClosed()
      .pipe(
        filter((confirmed) => confirmed === true),
        switchMap(() => this.tasksApi.delete(task.id)),
      )
      .subscribe({
        next: () => {
          this.notifier.success('Task deleted');
          this.changed.emit();
          this.closed.emit();
        },
        error: alreadyNotified,
      });
  }

  protected addRelated(relatedTaskId: string): void {
    const task = this.task();
    if (task) {
      this.replaceRelated(task, [...task.relatedTasks.map((related) => related.id), relatedTaskId]);
    }
  }

  protected removeRelated(relatedTaskId: string): void {
    const task = this.task();
    if (task) {
      this.replaceRelated(
        task,
        task.relatedTasks.map((related) => related.id).filter((id) => id !== relatedTaskId),
      );
    }
  }

  private replaceRelated(task: TaskDetails, relatedTaskIds: string[]): void {
    this.run(
      this.tasksApi.replaceRelatedTasks(task.id, relatedTaskIds),
      'Related tasks updated',
      false,
    );
  }

  private run(operation: Observable<void>, successMessage: string, affectsBoard: boolean): void {
    if (this.busy()) {
      return;
    }

    this.busy.set(true);
    operation.subscribe({
      next: () => {
        this.busy.set(false);
        this.notifier.success(successMessage);
        this.reload$.next();
        if (affectsBoard) {
          this.changed.emit();
        }
      },
      error: () => this.busy.set(false),
    });
  }

  private showTask(task: TaskDetails): void {
    this.task.set(task);
    this.form.reset({
      title: task.title,
      description: task.description ?? '',
      assigneeId: task.assignee?.id ?? null,
    });

    if (isCoreEditable(task.status)) {
      this.form.enable();
    } else {
      this.form.disable();
    }
  }

  private isTask(predicate: (status: TaskDetails['status']) => boolean): boolean {
    const task = this.task();
    return !!task && predicate(task.status);
  }
}
