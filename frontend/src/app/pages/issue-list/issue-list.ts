import { DatePipe } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Issue, IssuePriority, IssueStatus, Project } from '../../core/models/api.models';
import { Api } from '../../core/services/api';

@Component({
  selector: 'app-issue-list',
  imports: [DatePipe, FormsModule, RouterLink],
  templateUrl: './issue-list.html'
})
export class IssueList implements OnInit {
  readonly statuses: IssueStatus[] = ['Open', 'InProgress', 'Fixed', 'Rejected'];
  readonly priorities: IssuePriority[] = ['Low', 'Medium', 'High', 'Critical'];

  issues: Issue[] = [];
  projects: Project[] = [];
  isLoading = true;
  error = '';

  filters: { status: IssueStatus | ''; priority: IssuePriority | ''; projectId: number | '' } = {
    status: '',
    priority: '',
    projectId: ''
  };

  constructor(private readonly api: Api) {}

  ngOnInit(): void {
    this.api.getProjects().subscribe({ next: projects => (this.projects = projects) });
    this.loadIssues();
  }

  loadIssues(): void {
    this.isLoading = true;
    this.api.getIssues(this.filters).subscribe({
      next: issues => {
        this.issues = issues;
        this.isLoading = false;
      },
      error: () => {
        this.error = 'Could not load issues.';
        this.isLoading = false;
      }
    });
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
      next: () => this.loadIssues(),
      error: () => (this.error = 'Could not delete issue.')
    });
  }
}
