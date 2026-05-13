import { Component, OnInit } from '@angular/core';
import { MemberStats } from '../../core/models/api.models';
import { Api } from '../../core/services/api';

@Component({
  selector: 'app-stats',
  templateUrl: './stats.html'
})
export class StatsPage implements OnInit {
  stats: MemberStats[] = [];
  isLoading = true;
  error = '';

  constructor(private readonly api: Api) {}

  ngOnInit(): void {
    const teamId = this.api.getActiveTeamId();
    if (!teamId) {
      this.error = 'Choose a team first.';
      this.isLoading = false;
      return;
    }

    this.api.getTeamStats(teamId).subscribe({
      next: stats => {
        this.stats = stats;
        this.isLoading = false;
      },
      error: () => {
        this.error = 'Could not load team statistics.';
        this.isLoading = false;
      }
    });
  }
}
