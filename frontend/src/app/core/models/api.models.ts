export type IssueStatus = 'Open' | 'InProgress' | 'Fixed' | 'Rejected';
export type IssuePriority = 'Low' | 'Medium' | 'High' | 'Critical';

export interface Project {
  id: number;
  name: string;
  description?: string;
  issueCount: number;
}

export interface ProjectPayload {
  name: string;
  description?: string;
}

export interface Issue {
  id: number;
  title: string;
  description: string;
  projectId: number;
  projectName: string;
  status: IssueStatus;
  priority: IssuePriority;
  createdAt: string;
  updatedAt: string;
  commentCount: number;
}

export interface IssuePayload {
  title: string;
  description: string;
  projectId: number;
  status: IssueStatus;
  priority: IssuePriority;
}

export interface Comment {
  id: number;
  issueId: number;
  text: string;
  author: string;
  createdAt: string;
}

export interface CommentPayload {
  text: string;
  author: string;
}

export interface DashboardSummary {
  totalIssues: number;
  openIssues: number;
  fixedIssues: number;
  criticalIssues: number;
  issuesByStatus: Array<{ status: IssueStatus; count: number }>;
}

export interface AuthStatus {
  isAuthenticated: boolean;
  googleConfigured: boolean;
  name?: string;
  email?: string;
  userId?: number;
}

export interface AuthPayload {
  email: string;
  password: string;
  displayName?: string;
}

export interface TeamMember {
  id: number;
  displayName: string;
  email: string;
  role: string;
  joinedAt: string;
}

export interface Team {
  id: number;
  name: string;
  inviteCode: string;
  createdAt: string;
  projectCount: number;
  members: TeamMember[];
}
