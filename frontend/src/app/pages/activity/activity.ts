import { DatePipe } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivityLog } from '../../core/models/api.models';
import { Api } from '../../core/services/api';

@Component({
  selector: 'app-activity',
  imports: [DatePipe],
  templateUrl: './activity.html'
})
export class ActivityPage implements OnInit {
  activity: ActivityLog[] = [];
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

    this.api.getTeamActivity(teamId).subscribe({
      next: activity => {
        this.activity = activity;
        this.isLoading = false;
      },
      error: () => {
        this.error = 'Could not load activity log.';
        this.isLoading = false;
      }
    });
  }
}
