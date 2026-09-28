import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, catchError, forkJoin, of, switchMap, tap } from 'rxjs';
import { alreadyNotified } from '../../../core/http/error-notification.interceptor';
import { ProjectsApi } from '../../projects/data-access/projects-api';
import { Project } from '../../projects/models/project.models';
import { TasksApi } from '../../tasks/data-access/tasks-api';
import { TaskStatus, TaskSummary } from '../../tasks/models/task.models';
import { STATUS_LABELS, TASK_STATUSES } from '../../tasks/models/task-workflow';
import { UsersApi } from '../../users/data-access/users-api';
import { User } from '../../users/models/user.models';

export interface BoardColumn {
  status: TaskStatus;
  label: string;
  tasks: TaskSummary[];
}

/**
 * Dashboard state. Project and assignee filters are applied by the API; the board groups the
 * single task-list response into status columns, hiding Closed unless requested.
 */
@Injectable()
export class DashboardStore {
  private readonly tasksApi = inject(TasksApi);
  private readonly usersApi = inject(UsersApi);
  private readonly projectsApi = inject(ProjectsApi);
  private readonly reloadTasks$ = new Subject<void>();

  readonly users = signal<User[]>([]);
  readonly projects = signal<Project[]>([]);
  readonly tasks = signal<TaskSummary[]>([]);
  readonly projectFilter = signal<string | null>(null);
  readonly assigneeFilter = signal<string | null>(null);
  readonly showClosed = signal(false);
  readonly loading = signal(false);

  readonly columns = computed<BoardColumn[]>(() => {
    const tasks = this.tasks();
    const statuses = this.showClosed()
      ? TASK_STATUSES
      : TASK_STATUSES.filter((status) => status !== 'Closed');

    return statuses.map((status) => ({
      status,
      label: STATUS_LABELS[status],
      tasks: tasks.filter((task) => task.status === status),
    }));
  });

  constructor() {
    this.reloadTasks$
      .pipe(
        tap(() => this.loading.set(true)),
        switchMap(() =>
          this.tasksApi
            .list({ projectId: this.projectFilter(), assigneeId: this.assigneeFilter() })
            .pipe(catchError(() => of(null))),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((tasks) => {
        this.loading.set(false);
        if (tasks) {
          this.tasks.set(tasks);
        }
      });
  }

  loadAll(): void {
    this.loadReferenceData();
    this.loadTasks();
  }

  loadTasks(): void {
    this.reloadTasks$.next();
  }

  setProjectFilter(projectId: string | null): void {
    this.projectFilter.set(projectId);
    this.loadTasks();
  }

  setAssigneeFilter(assigneeId: string | null): void {
    this.assigneeFilter.set(assigneeId);
    this.loadTasks();
  }

  private loadReferenceData(): void {
    forkJoin({ users: this.usersApi.list(), projects: this.projectsApi.list() }).subscribe({
      next: ({ users, projects }) => {
        this.users.set(users);
        this.projects.set(projects);
        this.clearStaleFilters(users, projects);
      },
      error: alreadyNotified,
    });
  }

  /** Drops a filter whose user or project was deleted, so the board does not stay empty. */
  private clearStaleFilters(users: User[], projects: Project[]): void {
    const project = this.projectFilter();
    const assignee = this.assigneeFilter();
    const projectGone = project !== null && !projects.some((p) => p.id === project);
    const assigneeGone = assignee !== null && !users.some((u) => u.id === assignee);

    if (projectGone) {
      this.projectFilter.set(null);
    }
    if (assigneeGone) {
      this.assigneeFilter.set(null);
    }
    if (projectGone || assigneeGone) {
      this.loadTasks();
    }
  }
}
