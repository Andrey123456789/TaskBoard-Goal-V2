import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '../../../../core/api/api-base-url';
import { TaskDetails, TaskSummary } from '../../models/task.models';
import { TaskDetailPanel } from './task-detail-panel';

describe('TaskDetailPanel', () => {
  let http: HttpTestingController;

  const related: TaskSummary = {
    id: 'r1',
    title: 'Related work',
    status: 'Created',
    project: { id: 'p2', name: 'Mobile' },
    assignee: null,
  };
  const other: TaskSummary = { ...related, id: 'o1', title: 'Other work' };

  function details(overrides: Partial<TaskDetails>): TaskDetails {
    return {
      id: 't1',
      title: 'Ship release',
      description: 'Release notes',
      status: 'Created',
      project: { id: 'p1', name: 'Website' },
      assignee: { id: 'u1', name: 'Alice', isDeletedUser: false },
      createdAt: '2026-09-01T10:00:00Z',
      updatedAt: '2026-09-02T10:00:00Z',
      relatedTasks: [related],
      ...overrides,
    };
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: '' },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function render(task: TaskDetails): Promise<ComponentFixture<TaskDetailPanel>> {
    const fixture = TestBed.createComponent(TaskDetailPanel);
    fixture.componentRef.setInput('taskId', task.id);
    fixture.componentRef.setInput('users', [
      { id: 'u1', name: 'Alice', email: 'a@x', createdAt: '' },
    ]);
    await fixture.whenStable();
    await flushLoad(fixture, task);
    return fixture;
  }

  async function flushLoad(
    fixture: ComponentFixture<TaskDetailPanel>,
    task: TaskDetails,
  ): Promise<void> {
    http.expectOne(`/api/tasks/${task.id}`).flush(task);
    http.expectOne('/api/tasks').flush([task, related, other]);
    await fixture.whenStable();
  }

  function element(fixture: ComponentFixture<TaskDetailPanel>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  it('lets a Created task be edited and deleted', async () => {
    const fixture = await render(details({}));

    expect(element(fixture).querySelector('form.edit-form')).not.toBeNull();
    expect(element(fixture).querySelector('button.delete-task')).not.toBeNull();
    expect(element(fixture).querySelector('button.create-related')).toBeNull();
    expect(element(fixture).querySelector('button.status-action')?.textContent?.trim()).toBe(
      'Start',
    );
  });

  it('saves edited core fields and sends an empty description as null', async () => {
    const fixture = await render(details({}));
    const title = element(fixture).querySelector<HTMLInputElement>('input[formcontrolname=title]')!;
    const description = element(fixture).querySelector<HTMLTextAreaElement>(
      'textarea[formcontrolname=description]',
    )!;
    title.value = 'Ship release v2';
    title.dispatchEvent(new Event('input'));
    description.value = '';
    description.dispatchEvent(new Event('input'));
    await fixture.whenStable();

    element(fixture)
      .querySelector<HTMLButtonElement>('form.edit-form button[type=submit]')!
      .click();

    const update = http.expectOne('/api/tasks/t1');
    expect(update.request.method).toBe('PUT');
    expect(update.request.body).toEqual({
      title: 'Ship release v2',
      description: null,
      assigneeId: 'u1',
    });
    update.flush(null, { status: 204, statusText: 'No Content' });
    await flushLoad(fixture, details({ title: 'Ship release v2', description: null }));
  });

  it('shows a Completed task read-only with a follow-up action and editable relations', async () => {
    const fixture = await render(details({ status: 'Completed' }));
    const created: TaskDetails[] = [];
    fixture.componentInstance.createRelatedRequested.subscribe((task) => created.push(task));

    expect(element(fixture).querySelector('form.edit-form')).toBeNull();
    expect(element(fixture).textContent).toContain('Completed and closed tasks are read-only.');
    expect(element(fixture).querySelector('button.delete-task')).toBeNull();
    expect(element(fixture).querySelector('button.status-action')?.textContent?.trim()).toBe(
      'Close',
    );

    element(fixture).querySelector<HTMLButtonElement>('button.create-related')!.click();
    expect(created.map((task) => task.id)).toEqual(['t1']);

    element(fixture)
      .querySelector<HTMLButtonElement>('button[aria-label="Remove relation to Related work"]')!
      .click();
    const replace = http.expectOne('/api/tasks/t1/related-tasks');
    expect(replace.request.method).toBe('PUT');
    expect(replace.request.body).toEqual({ relatedTaskIds: [] });
    replace.flush(null, { status: 204, statusText: 'No Content' });
    await flushLoad(fixture, details({ status: 'Completed', relatedTasks: [] }));
  });

  it('shows a Closed task without any status action', async () => {
    const fixture = await render(details({ status: 'Closed' }));

    expect(element(fixture).querySelector('button.status-action')).toBeNull();
    expect(element(fixture).querySelector('button.create-related')).not.toBeNull();
  });
});
