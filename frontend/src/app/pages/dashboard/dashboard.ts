import { DatePipe } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Api } from '../../core/services/api';
import { ActivityLog, DashboardSummary, Issue } from '../../core/models/api.models';
import { forkJoin, of } from 'rxjs';

@Component({
  selector: 'app-dashboard',
  imports: [DatePipe, RouterLink],
  templateUrl: './dashboard.html'
})
export class Dashboard implements OnInit {
  summary?: DashboardSummary;
  recentIssues: Issue[] = [];
  criticalIssues: Issue[] = [];
  activity: ActivityLog[] = [];
  isLoading = true;
  error = '';

  constructor(private readonly api: Api) {}

  ngOnInit(): void {
    const activeTeamId = this.api.getActiveTeamId();

    forkJoin({
      summary: this.api.getDashboard(),
      issues: this.api.getIssues(),
      activity: activeTeamId ? this.api.getTeamActivity(activeTeamId) : of([])
    }).subscribe({
      next: ({ summary, issues, activity }) => {
        this.summary = summary;
        this.recentIssues = issues.slice(0, 4);
        this.criticalIssues = issues.filter(issue => issue.priority === 'Critical').slice(0, 3);
        this.activity = activity;
        this.isLoading = false;
      },
      error: () => {
        this.error = 'Could not load dashboard data. Make sure the backend is running.';
        this.isLoading = false;
      }
    });
  }

  recentActivityFor(issue: Issue): ActivityLog | undefined {
    return this.activity.find(log => log.issueId === issue.id);
  }
}
