export type TaskStatus = 'Created' | 'InProgress' | 'Completed' | 'Closed';

export interface ProjectSummary {
  id: string;
  name: string;
}

/** Current assignee; `isDeletedUser` marks the system "Deleted User". Unassigned tasks have none. */
export interface Assignee {
  id: string;
  name: string;
  isDeletedUser: boolean;
}

export interface TaskSummary {
  id: string;
  title: string;
  status: TaskStatus;
  project: ProjectSummary;
  assignee: Assignee | null;
}

export interface TaskDetails extends TaskSummary {
  description: string | null;
  createdAt: string;
  updatedAt: string;
  relatedTasks: TaskSummary[];
}

export interface TaskFilter {
  projectId?: string | null;
  assigneeId?: string | null;
  status?: TaskStatus | null;
}

export interface CreateTaskRequest {
  title: string;
  description: string | null;
  projectId: string;
  assigneeId: string | null;
  relatedTaskIds: string[];
}

export interface UpdateTaskRequest {
  title: string;
  description: string | null;
  assigneeId: string | null;
}
