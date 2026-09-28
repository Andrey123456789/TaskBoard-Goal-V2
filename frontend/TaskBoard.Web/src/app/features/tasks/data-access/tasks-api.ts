import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../../core/api/api-base-url';
import {
  CreateTaskRequest,
  TaskDetails,
  TaskFilter,
  TaskStatus,
  TaskSummary,
  UpdateTaskRequest,
} from '../models/task.models';

@Injectable({ providedIn: 'root' })
export class TasksApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/api/tasks`;

  /** One request returns everything the board needs (project and assignee names included). */
  list(filter: TaskFilter = {}): Observable<TaskSummary[]> {
    let params = new HttpParams();

    for (const [key, value] of Object.entries(filter)) {
      if (value) {
        params = params.set(key, value);
      }
    }

    return this.http.get<TaskSummary[]>(this.baseUrl, { params });
  }

  get(id: string): Observable<TaskDetails> {
    return this.http.get<TaskDetails>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateTaskRequest): Observable<TaskDetails> {
    return this.http.post<TaskDetails>(this.baseUrl, request);
  }

  update(id: string, request: UpdateTaskRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, request);
  }

  changeStatus(id: string, status: TaskStatus): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${id}/status`, { status });
  }

  /** Sends the complete desired related-task set (replace-set semantics). */
  replaceRelatedTasks(id: string, relatedTaskIds: string[]): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/related-tasks`, { relatedTaskIds });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
