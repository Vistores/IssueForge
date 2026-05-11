import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Api } from '../../core/services/api';
import { DashboardSummary } from '../../core/models/api.models';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink],
  templateUrl: './dashboard.html'
})
export class Dashboard implements OnInit {
  summary?: DashboardSummary;
  isLoading = true;
  error = '';

  constructor(private readonly api: Api) {}

  ngOnInit(): void {
    this.api.getDashboard().subscribe({
      next: summary => {
        this.summary = summary;
        this.isLoading = false;
      },
      error: () => {
        this.error = 'Could not load dashboard data. Make sure the backend is running.';
        this.isLoading = false;
      }
    });
  }
}
