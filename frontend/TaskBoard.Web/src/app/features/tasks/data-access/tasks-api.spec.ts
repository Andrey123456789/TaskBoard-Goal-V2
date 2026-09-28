import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '../../../core/api/api-base-url';
import { TasksApi } from './tasks-api';

describe('TasksApi', () => {
  let api: TasksApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'http://api' },
      ],
    });
    api = TestBed.inject(TasksApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lists tasks with only the filters that are set', () => {
    api.list({ projectId: 'p1', assigneeId: null, status: 'InProgress' }).subscribe();

    const request = http.expectOne((r) => r.url === 'http://api/api/tasks');
    expect(request.request.params.keys().sort()).toEqual(['projectId', 'status']);
    expect(request.request.params.get('projectId')).toBe('p1');
    expect(request.request.params.get('status')).toBe('InProgress');
    request.flush([]);
  });

  it('lists all tasks without filters', () => {
    api.list().subscribe();

    const request = http.expectOne('http://api/api/tasks');
    expect(request.request.params.keys()).toEqual([]);
    request.flush([]);
  });

  it('changes status with PATCH to the status sub-resource', () => {
    api.changeStatus('t1', 'Completed').subscribe();

    const request = http.expectOne('http://api/api/tasks/t1/status');
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ status: 'Completed' });
    request.flush(null, { status: 204, statusText: 'No Content' });
  });

  it('replaces the complete related-task set with PUT', () => {
    api.replaceRelatedTasks('t1', ['a', 'b']).subscribe();

    const request = http.expectOne('http://api/api/tasks/t1/related-tasks');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ relatedTaskIds: ['a', 'b'] });
    request.flush(null, { status: 204, statusText: 'No Content' });
  });
});
