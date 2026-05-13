import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Toast } from './core/services/toast';
import { Api } from './core/services/api';

@Component({
  selector: 'app-root',
  imports: [AsyncPipe, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css',
  encapsulation: ViewEncapsulation.None
})
export class App implements OnInit {
  activeTeamName = '';

  constructor(
    readonly toast: Toast,
    readonly router: Router,
    private readonly api: Api
  ) {}

  ngOnInit(): void {
    this.router.events.subscribe(() => this.loadActiveTeamName());
    this.loadActiveTeamName();
  }

  get isAccessRoute(): boolean {
    return this.router.url.startsWith('/auth');
  }

  private loadActiveTeamName(): void {
    if (this.isAccessRoute) {
      return;
    }

    this.api.getTeams().subscribe({
      next: teams => {
        const activeTeamId = this.api.getActiveTeamId();
        this.activeTeamName = teams.find(team => team.id === activeTeamId)?.name ?? teams[0]?.name ?? '';
      },
      error: () => (this.activeTeamName = '')
    });
  }
}
