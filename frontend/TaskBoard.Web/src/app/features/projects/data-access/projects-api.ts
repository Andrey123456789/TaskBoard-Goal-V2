import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../../core/api/api-base-url';
import { Project, SaveProjectRequest } from '../models/project.models';

@Injectable({ providedIn: 'root' })
export class ProjectsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/api/projects`;

  list(): Observable<Project[]> {
    return this.http.get<Project[]>(this.baseUrl);
  }

  create(request: SaveProjectRequest): Observable<Project> {
    return this.http.post<Project>(this.baseUrl, request);
  }

  update(id: string, request: SaveProjectRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
