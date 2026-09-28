import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { API_BASE_URL } from '../../../../core/api/api-base-url';
import { TaskSummary } from '../../../tasks/models/task.models';
import { DashboardStore } from '../../data-access/dashboard-store';
import { DashboardPage } from './dashboard-page';

describe('DashboardPage', () => {
  let http: HttpTestingController;
  let fixture: ComponentFixture<DashboardPage>;

  const project = { id: 'p1', name: 'Website' };
  const tasks: TaskSummary[] = [
    { id: 'c1', title: 'Created task', status: 'Created', project, assignee: null },
    { id: 'i1', title: 'Active task', status: 'InProgress', project, assignee: null },
    { id: 'd1', title: 'Done task', status: 'Completed', project, assignee: null },
    { id: 'x1', title: 'Closed task', status: 'Closed', project, assignee: null },
  ];

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: '' },
      ],
    });
    http = TestBed.inject(HttpTestingController);

    fixture = TestBed.createComponent(DashboardPage);
    await fixture.whenStable();

    // The whole board loads with three requests: no per-project or per-user calls.
    http.expectOne('/api/users').flush([{ id: 'u1', name: 'Alice', email: 'a@x', createdAt: '' }]);
    http.expectOne('/api/projects').flush([{ ...project, description: null, createdAt: '' }]);
    http.expectOne('/api/tasks').flush(tasks);
    await fixture.whenStable();
  });

  afterEach(() => http.verify());

  function element(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function columnStatuses(): (string | undefined)[] {
    return [...element().querySelectorAll<HTMLElement>('section.column')].map(
      (c) => c.dataset['status'],
    );
  }

  function store(): DashboardStore {
    return fixture.debugElement.injector.get(DashboardStore);
  }

  it('shows Created, InProgress and Completed columns and hides closed tasks by default', () => {
    expect(columnStatuses()).toEqual(['Created', 'InProgress', 'Completed']);
    expect(element().textContent).toContain('Done task');
    expect(element().textContent).not.toContain('Closed task');
  });

  it('shows a Closed column when closed tasks are enabled', async () => {
    element().querySelector<HTMLButtonElement>('mat-slide-toggle button')!.click();
    await fixture.whenStable();

    expect(columnStatuses()).toEqual(['Created', 'InProgress', 'Completed', 'Closed']);
    expect(element().textContent).toContain('Closed task');
  });

  it('filters the board on the server by project and assignee', async () => {
    store().setProjectFilter('p1');
    http.expectOne('/api/tasks?projectId=p1').flush([tasks[0]]);
    store().setAssigneeFilter('u1');
    http.expectOne('/api/tasks?projectId=p1&assigneeId=u1').flush([]);
    await fixture.whenStable();

    expect(element().querySelectorAll('app-task-card').length).toBe(0);
  });

  it('opens task details in the drawer on the same screen', async () => {
    element().querySelector<HTMLElement>('app-task-card mat-card')!.click();
    await fixture.whenStable();

    expect(element().querySelector('app-task-detail-panel')).not.toBeNull();
    http.expectOne('/api/tasks/c1').flush({
      ...tasks[0],
      description: null,
      createdAt: '2026-09-01T10:00:00Z',
      updatedAt: '2026-09-01T10:00:00Z',
      relatedTasks: [],
    });
    http.expectOne('/api/tasks').flush(tasks);
    await fixture.whenStable();

    expect(element().querySelector('app-task-detail-panel h2')?.textContent).toBe('Created task');
  });
});
