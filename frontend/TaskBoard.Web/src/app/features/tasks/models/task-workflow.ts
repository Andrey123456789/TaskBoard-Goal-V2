import { TaskStatus, TaskSummary } from './task.models';

/**
 * UI mirror of the linear workflow Created -> InProgress -> Completed -> Closed, used only to
 * show the meaningful next action. The API remains authoritative and rejects anything else.
 */
export const TASK_STATUSES: readonly TaskStatus[] = [
  'Created',
  'InProgress',
  'Completed',
  'Closed',
];

export const STATUS_LABELS: Record<TaskStatus, string> = {
  Created: 'Created',
  InProgress: 'In Progress',
  Completed: 'Completed',
  Closed: 'Closed',
};

export interface StatusAction {
  label: string;
  target: TaskStatus;
}

const NEXT_ACTIONS: Record<TaskStatus, StatusAction | null> = {
  Created: { label: 'Start', target: 'InProgress' },
  InProgress: { label: 'Complete', target: 'Completed' },
  Completed: { label: 'Close', target: 'Closed' },
  Closed: null,
};

export function nextStatusAction(status: TaskStatus): StatusAction | null {
  return NEXT_ACTIONS[status];
}

/** Starting work requires an assigned active user (not unassigned, not "Deleted User"). */
export function hasActiveAssignee(task: TaskSummary): boolean {
  return task.assignee !== null && !task.assignee.isDeletedUser;
}

export function canStart(task: TaskSummary): boolean {
  return task.status !== 'Created' || hasActiveAssignee(task);
}

export function isCoreEditable(status: TaskStatus): boolean {
  return status === 'Created' || status === 'InProgress';
}

export function isDeletable(status: TaskStatus): boolean {
  return status === 'Created' || status === 'InProgress';
}

/** Completed and closed tasks are never reopened; follow-up work goes into a new related task. */
export function canCreateFollowUp(status: TaskStatus): boolean {
  return status === 'Completed' || status === 'Closed';
}
