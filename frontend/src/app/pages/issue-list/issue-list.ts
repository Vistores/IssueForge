import { CdkDragDrop, DragDropModule } from '@angular/cdk/drag-drop';
import { DatePipe } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { AuthStatus, Comment, Issue, IssueAttachmentPayload, IssuePriority, IssueStatus, Project, Team, TeamMember } from '../../core/models/api.models';
import { Api } from '../../core/services/api';
import { Toast } from '../../core/services/toast';

@Component({
  selector: 'app-issue-list',
  imports: [DatePipe, DragDropModule, FormsModule],
  templateUrl: './issue-list.html'
})
export class IssueList implements OnInit {
  readonly statuses: IssueStatus[] = ['Open', 'InProgress', 'Fixed', 'Rejected'];
  readonly priorities: IssuePriority[] = ['Low', 'Medium', 'High', 'Critical'];
  readonly boardDropLists = this.statuses.map(status => `board-${status}`);

  issues: Issue[] = [];
  projects: Project[] = [];
  teams: Team[] = [];
  members: TeamMember[] = [];
  auth?: AuthStatus;
  selectedIssue?: Issue;
  selectedComments: Comment[] = [];
  selectedAssigneeIssue?: Issue;
  modalIssue?: Issue | null;
  issueForm = {
    title: '',
    description: '',
    projectId: 0,
    status: 'Open' as IssueStatus,
    priority: 'Medium' as IssuePriority,
    assignedMemberIds: [] as number[],
    attachments: [] as IssueAttachmentPayload[]
  };
  isPreviewLoading = false;
  viewMode: 'board' | 'table' = 'board';
  isLoading = true;
  error = '';
  isUpdating = false;
  isAssigneeFilterOpen = false;
  commentForm = {
    text: ''
  };

  filters: { status: IssueStatus | ''; priority: IssuePriority | ''; projectId: number | ''; assigneeId: number | ''; teamId: number | '' } = {
    status: '',
    priority: '',
    projectId: '',
    assigneeId: '',
    teamId: ''
  };

  constructor(
    private readonly api: Api,
    private readonly toast: Toast,
    private readonly route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.api.getAuthStatus().subscribe({
      next: auth => {
        this.auth = auth;
      }
    });
    this.api.getTeams().subscribe({
      next: teams => {
        this.teams = teams;
        const activeTeamId = this.api.getActiveTeamId();
        const selectedTeam = teams.find(team => team.id === activeTeamId);
        this.filters.teamId = selectedTeam?.id ?? '';
        this.members = selectedTeam?.members ?? teams.flatMap(team => team.members);
        this.api.setActiveTeamId(selectedTeam?.id ?? null);
        this.loadProjects();
        this.loadIssues();
      }
    });
  }

  loadProjects(): void {
    this.api.getProjects().subscribe({ next: projects => (this.projects = projects) });
  }

  loadIssues(): void {
    this.isLoading = true;
    this.api.getIssues(this.filters).subscribe({
      next: issues => {
        this.issues = issues;
        this.openRequestedPreview();
        this.error = '';
        this.isLoading = false;
      },
      error: () => {
        this.error = 'Could not load issues.';
        this.isLoading = false;
      }
    });
  }

  changeTeamFilter(teamId: number | ''): void {
    this.filters.teamId = teamId;
    this.filters.projectId = '';
    this.filters.assigneeId = '';
    const selectedTeam = this.teams.find(team => team.id === teamId);
    this.members = selectedTeam?.members ?? this.teams.flatMap(team => team.members);
    this.api.setActiveTeamId(selectedTeam?.id ?? null);
    this.loadProjects();
    this.loadIssues();
  }

  selectAssigneeFilter(memberId: number | ''): void {
    this.filters.assigneeId = memberId;
    this.isAssigneeFilterOpen = false;
    this.loadIssues();
  }

  get selectedAssignee(): TeamMember | undefined {
    return this.members.find(member => member.id === this.filters.assigneeId);
  }

  issuesByStatus(status: IssueStatus): Issue[] {
    return this.issues.filter(issue => issue.status === status);
  }

  dropIssue(event: CdkDragDrop<Issue[]>, status: IssueStatus): void {
    const issue = event.item.data as Issue;
    if (!issue || issue.status === status) {
      return;
    }

    this.changeStatus(issue, status);
  }

  changeStatus(issue: Issue, status: string): void {
    if (!this.isIssueStatus(status) || issue.status === status) {
      return;
    }

    this.saveIssueChange(issue, { status });
  }

  changePriority(issue: Issue, priority: string): void {
    if (!this.isIssuePriority(priority) || issue.priority === priority) {
      return;
    }

    this.saveIssueChange(issue, { priority });
  }

  openPreview(issue: Issue): void {
    this.selectedIssue = issue;
    this.selectedComments = [];
    this.commentForm.text = '';
    this.isPreviewLoading = true;
    this.api.getComments(issue.id).subscribe({
      next: comments => {
        this.selectedComments = comments;
        this.isPreviewLoading = false;
      },
      error: () => {
        this.isPreviewLoading = false;
        this.toast.error('Could not load comments.');
      }
    });
  }

  private openRequestedPreview(): void {
    const previewId = Number(this.route.snapshot.queryParamMap.get('previewId'));
    if (!previewId || this.selectedIssue?.id === previewId) {
      return;
    }

    const issue = this.issues.find(item => item.id === previewId);
    if (issue) {
      this.openPreview(issue);
    }
  }

  openCreateModal(): void {
    this.modalIssue = null;
    this.issueForm = {
      title: '',
      description: '',
      projectId: this.projects[0]?.id ?? 0,
      status: 'Open',
      priority: 'Medium',
      assignedMemberIds: [],
      attachments: []
    };
  }

  openEditModal(issue: Issue): void {
    this.modalIssue = issue;
    this.issueForm = {
      title: issue.title,
      description: issue.description,
      projectId: issue.projectId,
      status: issue.status,
      priority: issue.priority,
      assignedMemberIds: issue.assignees.map(assignee => assignee.memberId),
      attachments: issue.attachments.map(attachment => ({
        fileName: attachment.fileName,
        contentType: attachment.contentType,
        size: attachment.size,
        dataUrl: attachment.dataUrl
      }))
    };
  }

  closeIssueModal(): void {
    this.modalIssue = undefined;
  }

  saveIssue(): void {
    if (!this.issueForm.title.trim() || !this.issueForm.description.trim() || !this.issueForm.projectId) {
      this.toast.error('Title, description and project are required.');
      return;
    }

    const payload = {
      ...this.issueForm,
      title: this.issueForm.title.trim(),
      description: this.issueForm.description.trim()
    };

    if (this.modalIssue) {
      this.api.updateIssue(this.modalIssue.id, payload).subscribe({
        next: () => {
          this.toast.success('Issue updated.');
          this.closeIssueModal();
          this.loadIssues();
        },
        error: () => this.toast.error('Could not save issue.')
      });
      return;
    }

    this.api.createIssue(payload).subscribe({
      next: () => {
        this.toast.success('Issue created.');
        this.closeIssueModal();
        this.loadIssues();
      },
      error: () => this.toast.error('Could not create issue.')
    });
  }

  toggleAssignee(memberId: number, checked: boolean): void {
    if (checked) {
      this.issueForm.assignedMemberIds = [...new Set([...this.issueForm.assignedMemberIds, memberId])];
      return;
    }

    this.issueForm.assignedMemberIds = this.issueForm.assignedMemberIds.filter(id => id !== memberId);
  }

  isAssigned(memberId: number): boolean {
    return this.issueForm.assignedMemberIds.includes(memberId);
  }

  handleIssueFiles(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    files.slice(0, 8 - this.issueForm.attachments.length).forEach(file => this.addIssueFile(file));
    input.value = '';
  }

  removeIssueAttachment(fileName: string): void {
    this.issueForm.attachments = this.issueForm.attachments.filter(attachment => attachment.fileName !== fileName);
  }

  formatFileSize(size: number): string {
    if (size < 1024) {
      return `${size} B`;
    }

    if (size < 1024 * 1024) {
      return `${(size / 1024).toFixed(1)} KB`;
    }

    return `${(size / 1024 / 1024).toFixed(1)} MB`;
  }

  openAssignees(issue: Issue, event?: Event): void {
    event?.stopPropagation();
    this.selectedAssigneeIssue = issue;
  }

  closeAssignees(): void {
    this.selectedAssigneeIssue = undefined;
  }

  closePreview(): void {
    this.selectedIssue = undefined;
    this.selectedComments = [];
  }

  copyDescription(issue: Issue): void {
    navigator.clipboard
      .writeText(issue.description)
      .then(() => this.toast.success('Description copied.'))
      .catch(() => this.toast.error('Could not copy description.'));
  }

  copyComment(comment: Comment): void {
    navigator.clipboard
      .writeText(comment.text)
      .then(() => this.toast.success('Comment copied.'))
      .catch(() => this.toast.error('Could not copy comment.'));
  }

  addCommentFromPreview(): void {
    if (!this.selectedIssue || !this.commentForm.text.trim()) {
      this.toast.error('Comment text is required.');
      return;
    }

    this.api
      .addComment(this.selectedIssue.id, {
        text: this.commentForm.text.trim()
      })
      .subscribe({
        next: comment => {
          this.selectedComments = [...this.selectedComments, comment];
          this.selectedIssue = {
            ...this.selectedIssue!,
            commentCount: this.selectedIssue!.commentCount + 1,
            updatedAt: new Date().toISOString()
          };
          this.issues = this.issues.map(issue => (issue.id === this.selectedIssue!.id ? this.selectedIssue! : issue));
          this.commentForm.text = '';
          this.toast.success('Comment added.');
        },
        error: () => this.toast.error('Could not add comment.')
      });
  }

  deletePreviewComment(comment: Comment): void {
    if (!this.selectedIssue || !confirm('Delete this comment?')) {
      return;
    }

    this.api.deleteComment(this.selectedIssue.id, comment.id).subscribe({
      next: () => {
        this.selectedComments = this.selectedComments.filter(item => item.id !== comment.id);
        this.selectedIssue = {
          ...this.selectedIssue!,
          commentCount: Math.max(0, this.selectedIssue!.commentCount - 1),
          updatedAt: new Date().toISOString()
        };
        this.issues = this.issues.map(issue => (issue.id === this.selectedIssue!.id ? this.selectedIssue! : issue));
        this.toast.success('Comment deleted.');
      },
      error: () => this.toast.error('Could not delete comment.')
    });
  }

  toggleIssueAssignee(issue: Issue, member: TeamMember, event?: Event): void {
    event?.stopPropagation();
    const assigned = issue.assignees.some(assignee => assignee.memberId === member.id);
    const assignedMemberIds = assigned
      ? issue.assignees.map(assignee => assignee.memberId).filter(id => id !== member.id)
      : [...issue.assignees.map(assignee => assignee.memberId), member.id];

    this.saveIssueAssignments(issue, assignedMemberIds);
  }

  assignSelf(issue: Issue, event?: Event): void {
    event?.stopPropagation();
    const currentMember = this.members.find(member => member.userId === this.auth?.userId);
    if (!currentMember) {
      this.toast.error('Your team member profile was not found.');
      return;
    }

    if (issue.assignees.some(assignee => assignee.memberId === currentMember.id)) {
      this.toast.success('You are already assigned.');
      return;
    }

    this.saveIssueAssignments(issue, [...issue.assignees.map(assignee => assignee.memberId), currentMember.id]);
  }

  isIssueAssignedTo(issue: Issue, memberId: number): boolean {
    return issue.assignees.some(assignee => assignee.memberId === memberId);
  }

  clearFilters(): void {
    this.filters = { ...this.filters, status: '', priority: '', projectId: '', assigneeId: '' };
    this.isAssigneeFilterOpen = false;
    this.loadIssues();
  }

  deleteIssue(issue: Issue): void {
    if (!confirm(`Delete issue "${issue.title}"?`)) {
      return;
    }

    this.api.deleteIssue(issue.id).subscribe({
      next: () => {
        this.toast.success('Issue deleted.');
        this.loadIssues();
      },
      error: () => {
        this.error = 'Could not delete issue.';
        this.toast.error(this.error);
      }
    });
  }

  private saveIssueChange(issue: Issue, patch: Partial<Pick<Issue, 'status' | 'priority'>>): void {
    const previous = { status: issue.status, priority: issue.priority };
    const updated = { ...issue, ...patch };

    issue.status = updated.status;
    issue.priority = updated.priority;
    this.isUpdating = true;

    this.api
      .updateIssue(issue.id, {
        title: issue.title,
        description: issue.description,
        projectId: issue.projectId,
        status: updated.status,
        priority: updated.priority,
        assignedMemberIds: issue.assignees.map(assignee => assignee.memberId)
      })
      .subscribe({
        next: () => {
          issue.updatedAt = new Date().toISOString();
          this.isUpdating = false;
          this.toast.success('Issue updated.');
        },
        error: () => {
          issue.status = previous.status;
          issue.priority = previous.priority;
          this.isUpdating = false;
          this.toast.error('Could not update issue.');
        }
      });
  }

  private saveIssueAssignments(issue: Issue, assignedMemberIds: number[]): void {
    this.isUpdating = true;
    this.api
      .updateIssue(issue.id, {
        title: issue.title,
        description: issue.description,
        projectId: issue.projectId,
        status: issue.status,
        priority: issue.priority,
        assignedMemberIds
      })
      .subscribe({
        next: () => {
          const assignees = this.members
            .filter(member => assignedMemberIds.includes(member.id))
            .map(member => ({
              memberId: member.id,
              displayName: member.displayName,
              role: member.role,
              avatarUrl: member.avatarUrl
            }));
          const updatedIssue = { ...issue, assignees, updatedAt: new Date().toISOString() };
          this.issues = this.issues.map(item => (item.id === issue.id ? updatedIssue : item));
          if (this.selectedIssue?.id === issue.id) {
            this.selectedIssue = updatedIssue;
          }
          if (this.selectedAssigneeIssue?.id === issue.id) {
            this.selectedAssigneeIssue = updatedIssue;
          }
          this.isUpdating = false;
          this.toast.success('Assignees updated.');
        },
        error: () => {
          this.isUpdating = false;
          this.toast.error('Could not update assignees.');
        }
      });
  }

  private addIssueFile(file: File): void {
    if (file.size > 3 * 1024 * 1024) {
      this.toast.error(`${file.name} is larger than 3 MB.`);
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      this.issueForm.attachments = [
        ...this.issueForm.attachments,
        {
          fileName: file.name,
          contentType: file.type || 'application/octet-stream',
          size: file.size,
          dataUrl: String(reader.result ?? '')
        }
      ].slice(0, 8);
    };
    reader.readAsDataURL(file);
  }

  private isIssueStatus(value: string): value is IssueStatus {
    return this.statuses.includes(value as IssueStatus);
  }

  private isIssuePriority(value: string): value is IssuePriority {
    return this.priorities.includes(value as IssuePriority);
  }
}
