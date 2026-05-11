import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import {
  Comment,
  CommentPayload,
  DashboardSummary,
  Issue,
  IssuePayload,
  IssuePriority,
  IssueStatus,
  Project,
  ProjectPayload
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class Api {
  private readonly baseUrl = 'http://localhost:5008/api';

  constructor(private readonly http: HttpClient) {}

  getDashboard() {
    return this.http.get<DashboardSummary>(`${this.baseUrl}/dashboard`);
  }

  getProjects() {
    return this.http.get<Project[]>(`${this.baseUrl}/projects`);
  }

  getProject(id: number) {
    return this.http.get<Project>(`${this.baseUrl}/projects/${id}`);
  }

  createProject(payload: ProjectPayload) {
    return this.http.post<Project>(`${this.baseUrl}/projects`, payload);
  }

  updateProject(id: number, payload: ProjectPayload) {
    return this.http.put<void>(`${this.baseUrl}/projects/${id}`, payload);
  }

  deleteProject(id: number) {
    return this.http.delete<void>(`${this.baseUrl}/projects/${id}`);
  }

  getIssues(filters: { status?: IssueStatus | ''; priority?: IssuePriority | ''; projectId?: number | '' } = {}) {
    let params = new HttpParams();

    if (filters.status) {
      params = params.set('status', filters.status);
    }

    if (filters.priority) {
      params = params.set('priority', filters.priority);
    }

    if (filters.projectId) {
      params = params.set('projectId', filters.projectId);
    }

    return this.http.get<Issue[]>(`${this.baseUrl}/issues`, { params });
  }

  getIssue(id: number) {
    return this.http.get<Issue>(`${this.baseUrl}/issues/${id}`);
  }

  createIssue(payload: IssuePayload) {
    return this.http.post<Issue>(`${this.baseUrl}/issues`, payload);
  }

  updateIssue(id: number, payload: IssuePayload) {
    return this.http.put<void>(`${this.baseUrl}/issues/${id}`, payload);
  }

  deleteIssue(id: number) {
    return this.http.delete<void>(`${this.baseUrl}/issues/${id}`);
  }

  getComments(issueId: number) {
    return this.http.get<Comment[]>(`${this.baseUrl}/issues/${issueId}/comments`);
  }

  addComment(issueId: number, payload: CommentPayload) {
    return this.http.post<Comment>(`${this.baseUrl}/issues/${issueId}/comments`, payload);
  }

  deleteComment(issueId: number, commentId: number) {
    return this.http.delete<void>(`${this.baseUrl}/issues/${issueId}/comments/${commentId}`);
  }
}
