import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import {
  Comment,
  CommentPayload,
  AuthStatus,
  AuthPayload,
  DashboardSummary,
  Issue,
  IssuePayload,
  IssuePriority,
  IssueStatus,
  Project,
  ProjectPayload,
  Team,
  TeamMemberUpdatePayload,
  TeamOwnerTransferPayload,
  MemberStats,
  ActivityLog
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class Api {
  private readonly baseUrl = 'http://localhost:5008/api';
  private readonly teamStorageKey = 'issueForge.activeTeamId';

  constructor(private readonly http: HttpClient) {}

  getDashboard() {
    return this.http.get<DashboardSummary>(`${this.baseUrl}/dashboard`, this.requestOptions());
  }

  getProjects() {
    return this.http.get<Project[]>(`${this.baseUrl}/projects`, this.requestOptions());
  }

  getProject(id: number) {
    return this.http.get<Project>(`${this.baseUrl}/projects/${id}`, this.requestOptions());
  }

  createProject(payload: ProjectPayload) {
    return this.http.post<Project>(`${this.baseUrl}/projects`, payload, this.requestOptions());
  }

  updateProject(id: number, payload: ProjectPayload) {
    return this.http.put<void>(`${this.baseUrl}/projects/${id}`, payload, this.requestOptions());
  }

  deleteProject(id: number) {
    return this.http.delete<void>(`${this.baseUrl}/projects/${id}`, this.requestOptions());
  }

  getIssues(filters: { status?: IssueStatus | ''; priority?: IssuePriority | ''; projectId?: number | ''; assigneeId?: number | '' } = {}) {
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

    if (filters.assigneeId) {
      params = params.set('assigneeId', filters.assigneeId);
    }

    return this.http.get<Issue[]>(`${this.baseUrl}/issues`, { ...this.requestOptions(), params });
  }

  getIssue(id: number) {
    return this.http.get<Issue>(`${this.baseUrl}/issues/${id}`, this.requestOptions());
  }

  createIssue(payload: IssuePayload) {
    return this.http.post<Issue>(`${this.baseUrl}/issues`, payload, this.requestOptions());
  }

  updateIssue(id: number, payload: IssuePayload) {
    return this.http.put<void>(`${this.baseUrl}/issues/${id}`, payload, this.requestOptions());
  }

  deleteIssue(id: number) {
    return this.http.delete<void>(`${this.baseUrl}/issues/${id}`, this.requestOptions());
  }

  getComments(issueId: number) {
    return this.http.get<Comment[]>(`${this.baseUrl}/issues/${issueId}/comments`, this.requestOptions());
  }

  addComment(issueId: number, payload: CommentPayload) {
    return this.http.post<Comment>(`${this.baseUrl}/issues/${issueId}/comments`, payload, this.requestOptions());
  }

  deleteComment(issueId: number, commentId: number) {
    return this.http.delete<void>(`${this.baseUrl}/issues/${issueId}/comments/${commentId}`, this.requestOptions());
  }

  getAuthStatus() {
    return this.http.get<AuthStatus>(`${this.baseUrl}/auth/status`, { withCredentials: true });
  }

  register(payload: AuthPayload) {
    return this.http.post<AuthStatus>(`${this.baseUrl}/auth/register`, payload, { withCredentials: true });
  }

  login(payload: AuthPayload) {
    return this.http.post<AuthStatus>(`${this.baseUrl}/auth/login`, payload, { withCredentials: true });
  }

  logout() {
    return this.http.post<void>(`${this.baseUrl}/auth/logout`, {}, { withCredentials: true });
  }

  updateAccount(payload: { avatarUrl?: string }) {
    return this.http.put<AuthStatus>(`${this.baseUrl}/auth/account`, payload, { withCredentials: true });
  }

  deleteAccount() {
    return this.http.delete<void>(`${this.baseUrl}/auth/account`, { withCredentials: true });
  }

  getGoogleLoginUrl(inviteCode?: string) {
    const params = inviteCode ? `?inviteCode=${encodeURIComponent(inviteCode)}` : '';
    return `${this.baseUrl}/auth/google${params}`;
  }

  getTeams() {
    return this.http.get<Team[]>(`${this.baseUrl}/teams`, { withCredentials: true });
  }

  createTeam(name: string) {
    return this.http.post<Team>(`${this.baseUrl}/teams`, { name }, { withCredentials: true });
  }

  joinTeam(inviteCode: string) {
    return this.http.post<Team>(`${this.baseUrl}/teams/join`, { inviteCode }, { withCredentials: true });
  }

  updateTeamMember(teamId: number, memberId: number, payload: TeamMemberUpdatePayload) {
    return this.http.put<void>(`${this.baseUrl}/teams/${teamId}/members/${memberId}`, payload, { withCredentials: true });
  }

  transferTeamOwner(teamId: number, payload: TeamOwnerTransferPayload) {
    return this.http.post<void>(`${this.baseUrl}/teams/${teamId}/transfer-owner`, payload, { withCredentials: true });
  }

  deleteTeam(teamId: number) {
    return this.http.delete<void>(`${this.baseUrl}/teams/${teamId}`, { withCredentials: true });
  }

  getTeamStats(teamId: number) {
    return this.http.get<MemberStats[]>(`${this.baseUrl}/teams/${teamId}/stats`, { withCredentials: true });
  }

  getTeamActivity(teamId: number) {
    return this.http.get<ActivityLog[]>(`${this.baseUrl}/teams/${teamId}/activity`, { withCredentials: true });
  }

  getActiveTeamId(): number | null {
    const value = localStorage.getItem(this.teamStorageKey);
    const parsed = value ? Number(value) : 0;
    return parsed > 0 ? parsed : null;
  }

  setActiveTeamId(teamId: number | null): void {
    if (teamId) {
      localStorage.setItem(this.teamStorageKey, String(teamId));
      return;
    }

    localStorage.removeItem(this.teamStorageKey);
  }

  private requestOptions() {
    const teamId = this.getActiveTeamId();
    const headers = teamId ? new HttpHeaders({ 'X-Team-Id': String(teamId) }) : undefined;

    return { withCredentials: true, headers };
  }
}
