import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TaskSummary } from '../../models/task.models';
import { TaskCard } from './task-card';

describe('TaskCard', () => {
  const baseTask: TaskSummary = {
    id: 't1',
    title: 'Write docs',
    status: 'Created',
    project: { id: 'p1', name: 'Website' },
    assignee: { id: 'u1', name: 'Alice', isDeletedUser: false },
  };

  async function render(task: TaskSummary): Promise<ComponentFixture<TaskCard>> {
    const fixture = TestBed.createComponent(TaskCard);
    fixture.componentRef.setInput('task', task);
    await fixture.whenStable();
    return fixture;
  }

  function text(fixture: ComponentFixture<TaskCard>): string {
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  function actionButton(fixture: ComponentFixture<TaskCard>): HTMLButtonElement | null {
    return (fixture.nativeElement as HTMLElement).querySelector('button.status-action');
  }

  it('shows title, project, assignee, status and the Start action', async () => {
    const fixture = await render(baseTask);

    expect(text(fixture)).toContain('Write docs');
    expect(text(fixture)).toContain('Website');
    expect(text(fixture)).toContain('Alice');
    expect(text(fixture)).toContain('Created');
    expect(actionButton(fixture)?.textContent?.trim()).toBe('Start');
    expect(actionButton(fixture)?.disabled).toBe(false);
  });

  it('shows an unassigned task as Unassigned and cannot start it', async () => {
    const fixture = await render({ ...baseTask, assignee: null });

    expect(text(fixture)).toContain('Unassigned');
    expect(actionButton(fixture)?.disabled).toBe(true);
  });

  it('distinguishes Deleted User from unassigned', async () => {
    const fixture = await render({
      ...baseTask,
      assignee: { id: 'd', name: 'Deleted User', isDeletedUser: true },
    });

    const assignee = (fixture.nativeElement as HTMLElement).querySelector('.assignee');
    expect(assignee?.textContent).toBe('Deleted User');
    expect(assignee?.classList).toContain('deleted-user');
    expect(actionButton(fixture)?.disabled).toBe(true);
  });

  it('requests the next status without opening the task', async () => {
    const fixture = await render({ ...baseTask, status: 'InProgress' });
    const statusRequests: unknown[] = [];
    const opened: string[] = [];
    fixture.componentInstance.statusChangeRequested.subscribe((r) => statusRequests.push(r));
    fixture.componentInstance.opened.subscribe((id) => opened.push(id));

    actionButton(fixture)!.click();

    expect(statusRequests).toEqual([{ taskId: 't1', target: 'Completed' }]);
    expect(opened).toEqual([]);
  });

  it('offers no status action for a closed task', async () => {
    const fixture = await render({ ...baseTask, status: 'Closed' });

    expect(actionButton(fixture)).toBeNull();
  });
});
