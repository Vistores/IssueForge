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
  assignees: IssueAssignee[];
  attachments: IssueAttachment[];
}

export interface IssueAssignee {
  memberId: number;
  displayName: string;
  role: string;
  avatarUrl?: string;
}

export interface IssuePayload {
  title: string;
  description: string;
  projectId: number;
  status: IssueStatus;
  priority: IssuePriority;
  assignedMemberIds: number[];
  attachments?: IssueAttachmentPayload[];
}

export interface IssueAttachment {
  id: number;
  fileName: string;
  contentType: string;
  size: number;
  dataUrl: string;
  createdAt: string;
}

export interface IssueAttachmentPayload {
  fileName: string;
  contentType: string;
  size: number;
  dataUrl: string;
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
  author?: string;
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
  avatarUrl?: string;
}

export interface AuthPayload {
  email: string;
  password: string;
  displayName?: string;
}

export interface TeamMember {
  id: number;
  userId: number;
  displayName: string;
  email: string;
  role: string;
  canEditIssues: boolean;
  canAssignIssues: boolean;
  issueLimit: number;
  avatarUrl?: string;
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

export interface TeamMemberUpdatePayload {
  role: string;
  canEditIssues: boolean;
  canAssignIssues: boolean;
  issueLimit: number;
}

export interface TeamOwnerTransferPayload {
  newOwnerMemberId: number;
}

export interface MemberStats {
  memberId: number;
  displayName: string;
  role: string;
  avatarUrl?: string;
  assignedIssues: number;
  openIssues: number;
  fixedIssues: number;
  criticalIssues: number;
}

export interface ActivityLog {
  id: number;
  action: string;
  details: string;
  actorName?: string;
  issueId?: number;
  issueTitle?: string;
  createdAt: string;
}
