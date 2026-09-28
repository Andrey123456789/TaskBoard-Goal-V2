import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatToolbarModule } from '@angular/material/toolbar';
import { filter, switchMap } from 'rxjs';
import { alreadyNotified } from '../../../../core/http/error-notification.interceptor';
import { Notifier } from '../../../../core/notifications/notifier';
import { ProjectsDialog } from '../../../projects/components/projects-dialog/projects-dialog';
import {
  CreateTaskDialog,
  CreateTaskDialogData,
} from '../../../tasks/components/create-task-dialog/create-task-dialog';
import { TaskCard } from '../../../tasks/components/task-card/task-card';
import { TaskDetailPanel } from '../../../tasks/components/task-detail-panel/task-detail-panel';
import { TasksApi } from '../../../tasks/data-access/tasks-api';
import { TaskDetails, TaskStatus, TaskSummary } from '../../../tasks/models/task.models';
import { STATUS_LABELS } from '../../../tasks/models/task-workflow';
import { UsersDialog } from '../../../users/components/users-dialog/users-dialog';
import { DashboardStore } from '../../data-access/dashboard-store';

/** The single TaskBoard screen: filters, status columns, task drawer, and management dialogs. */
@Component({
  selector: 'app-dashboard-page',
  imports: [
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatProgressBarModule,
    MatSelectModule,
    MatSidenavModule,
    MatSlideToggleModule,
    MatToolbarModule,
    TaskCard,
    TaskDetailPanel,
  ],
  providers: [DashboardStore],
  templateUrl: './dashboard-page.html',
  styleUrl: './dashboard-page.scss',
})
export class DashboardPage {
  protected readonly store = inject(DashboardStore);
  private readonly tasksApi = inject(TasksApi);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);

  protected readonly selectedTaskId = signal<string | null>(null);
  /** Bumped when something outside the drawer may have changed the selected task. */
  protected readonly panelRefreshKey = signal(0);

  constructor() {
    this.store.loadAll();
  }

  protected openTask(taskId: string): void {
    this.selectedTaskId.set(taskId);
  }

  protected closeTask(): void {
    this.selectedTaskId.set(null);
  }

  protected changeStatus(request: { taskId: string; target: TaskStatus }): void {
    this.tasksApi.changeStatus(request.taskId, request.target).subscribe({
      next: () => {
        this.notifier.success(`Task moved to ${STATUS_LABELS[request.target]}`);
        this.store.loadTasks();
        this.refreshPanel();
      },
      error: alreadyNotified,
    });
  }

  protected openNewTask(): void {
    this.openCreateTaskDialog();
  }

  protected createRelatedTask(source: TaskDetails): void {
    this.openCreateTaskDialog(source);
  }

  protected openUsers(): void {
    this.dialog
      .open(UsersDialog, { autoFocus: false })
      .afterClosed()
      .subscribe(() => this.refreshEverything());
  }

  protected openProjects(): void {
    this.dialog
      .open(ProjectsDialog, { autoFocus: false })
      .afterClosed()
      .subscribe(() => this.refreshEverything());
  }

  private openCreateTaskDialog(sourceTask?: TaskSummary): void {
    // Related-task choices come from all tasks, independent of the board filters.
    this.tasksApi
      .list()
      .pipe(
        switchMap((tasks) =>
          this.dialog
            .open<CreateTaskDialog, CreateTaskDialogData, TaskDetails>(CreateTaskDialog, {
              data: {
                projects: this.store.projects(),
                users: this.store.users(),
                tasks,
                sourceTask,
              },
            })
            .afterClosed(),
        ),
        filter((created): created is TaskDetails => !!created),
      )
      .subscribe({
        next: (created) => {
          this.notifier.success('Task created');
          this.store.loadTasks();
          this.selectedTaskId.set(created.id);
          this.refreshPanel();
        },
        error: alreadyNotified,
      });
  }

  private refreshEverything(): void {
    this.store.loadAll();
    this.refreshPanel();
  }

  private refreshPanel(): void {
    this.panelRefreshKey.update((key) => key + 1);
  }
}
