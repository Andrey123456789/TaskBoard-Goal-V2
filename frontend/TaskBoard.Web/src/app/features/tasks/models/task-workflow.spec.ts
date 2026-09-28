import { TaskStatus, TaskSummary } from './task.models';
import {
  canCreateFollowUp,
  canStart,
  isCoreEditable,
  isDeletable,
  nextStatusAction,
} from './task-workflow';

function task(status: TaskStatus, assignee: TaskSummary['assignee']): TaskSummary {
  return { id: 't1', title: 'Task', status, project: { id: 'p1', name: 'Project' }, assignee };
}

describe('task workflow (UI mirror)', () => {
  it('exposes only the meaningful next transition', () => {
    expect(nextStatusAction('Created')).toEqual({ label: 'Start', target: 'InProgress' });
    expect(nextStatusAction('InProgress')).toEqual({ label: 'Complete', target: 'Completed' });
    expect(nextStatusAction('Completed')).toEqual({ label: 'Close', target: 'Closed' });
    expect(nextStatusAction('Closed')).toBeNull();
  });

  it('allows starting only with an active assignee', () => {
    const alice = { id: 'u1', name: 'Alice', isDeletedUser: false };
    const deletedUser = { id: 'd', name: 'Deleted User', isDeletedUser: true };

    expect(canStart(task('Created', alice))).toBe(true);
    expect(canStart(task('Created', null))).toBe(false);
    expect(canStart(task('Created', deletedUser))).toBe(false);
    expect(canStart(task('InProgress', deletedUser))).toBe(true);
  });

  it('allows core editing and deletion only before completion', () => {
    expect(
      ['Created', 'InProgress', 'Completed', 'Closed'].map((s) => isCoreEditable(s as TaskStatus)),
    ).toEqual([true, true, false, false]);
    expect(
      ['Created', 'InProgress', 'Completed', 'Closed'].map((s) => isDeletable(s as TaskStatus)),
    ).toEqual([true, true, false, false]);
  });

  it('offers follow-up tasks only for completed and closed tasks', () => {
    expect(canCreateFollowUp('Completed')).toBe(true);
    expect(canCreateFollowUp('Closed')).toBe(true);
    expect(canCreateFollowUp('Created')).toBe(false);
    expect(canCreateFollowUp('InProgress')).toBe(false);
  });
});
