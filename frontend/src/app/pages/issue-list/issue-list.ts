import { CdkDragDrop, DragDropModule } from '@angular/cdk/drag-drop';
import { DatePipe } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Comment, Issue, IssuePriority, IssueStatus, Project, TeamMember } from '../../core/models/api.models';
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
  members: TeamMember[] = [];
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
    assignedMemberIds: [] as number[]
  };
  isPreviewLoading = false;
  viewMode: 'board' | 'table' = 'board';
  isLoading = true;
  error = '';
  isUpdating = false;

  filters: { status: IssueStatus | ''; priority: IssuePriority | ''; projectId: number | '' } = {
    status: '',
    priority: '',
    projectId: ''
  };

  constructor(
    private readonly api: Api,
    private readonly toast: Toast
  ) {}

  ngOnInit(): void {
    this.api.getProjects().subscribe({ next: projects => (this.projects = projects) });
    this.api.getTeams().subscribe({
      next: teams => {
        const activeTeamId = this.api.getActiveTeamId();
        this.members = teams.find(team => team.id === activeTeamId)?.members ?? teams[0]?.members ?? [];
      }
    });
    this.loadIssues();
  }

  loadIssues(): void {
    this.isLoading = true;
    this.api.getIssues(this.filters).subscribe({
      next: issues => {
        this.issues = issues;
        this.error = '';
        this.isLoading = false;
      },
      error: () => {
        this.error = 'Could not load issues.';
        this.isLoading = false;
      }
    });
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

  openCreateModal(): void {
    this.modalIssue = null;
    this.issueForm = {
      title: '',
      description: '',
      projectId: this.projects[0]?.id ?? 0,
      status: 'Open',
      priority: 'Medium',
      assignedMemberIds: []
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
      assignedMemberIds: issue.assignees.map(assignee => assignee.memberId)
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

  clearFilters(): void {
    this.filters = { status: '', priority: '', projectId: '' };
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

  private isIssueStatus(value: string): value is IssueStatus {
    return this.statuses.includes(value as IssueStatus);
  }

  private isIssuePriority(value: string): value is IssuePriority {
    return this.priorities.includes(value as IssuePriority);
  }
}
