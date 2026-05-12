import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthStatus, Team } from '../../core/models/api.models';
import { Api } from '../../core/services/api';
import { Toast } from '../../core/services/toast';

@Component({
  selector: 'app-team',
  imports: [FormsModule],
  templateUrl: './team.html'
})
export class TeamPage implements OnInit {
  auth?: AuthStatus;
  teams: Team[] = [];
  activeTeamId: number | null = null;
  teamName = 'New QA Guild';
  inviteCode = '';
  loginEmail = 'demo@game.local';
  loginPassword = 'Demo123!';
  registerName = '';
  registerEmail = '';
  registerPassword = '';
  authMode: 'login' | 'register' = 'login';
  isEntering = false;
  isLoading = true;
  error = '';

  constructor(
    private readonly api: Api,
    private readonly toast: Toast,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.activeTeamId = this.api.getActiveTeamId();
    this.api.getAuthStatus().subscribe({
      next: auth => {
        this.auth = auth;
        if (!auth.isAuthenticated) {
          this.teams = [];
          this.isLoading = false;
          return;
        }

        this.loadTeams();
      },
      error: () => {
        this.error = 'Could not load auth status.';
        this.isLoading = false;
      }
    });
  }

  loadTeams(redirectWhenReady = false): void {
    this.api.getTeams().subscribe({
      next: teams => {
        this.teams = teams;
        this.activeTeamId = this.resolveActiveTeam(teams);
        this.isLoading = false;
        if (redirectWhenReady && this.activeTeamId) {
          this.enterWorkspace();
        }
      },
      error: () => {
        this.error = 'Could not load teams.';
        this.isLoading = false;
      }
    });
  }

  login(): void {
    this.api.login({ email: this.loginEmail, password: this.loginPassword }).subscribe({
      next: auth => {
        this.auth = auth;
        this.toast.success('Signed in.');
        this.loadTeams(true);
      },
      error: () => this.toast.error('Wrong email or password.')
    });
  }

  register(): void {
    this.api
      .register({
        displayName: this.registerName,
        email: this.registerEmail,
        password: this.registerPassword
      })
      .subscribe({
        next: auth => {
          this.auth = auth;
          this.toast.success('Account created.');
          this.loadTeams();
        },
        error: () => this.toast.error('Could not create account.')
      });
  }

  loginWithGoogle(): void {
    window.location.href = this.api.getGoogleLoginUrl(this.inviteCode || undefined);
  }

  logout(): void {
    this.api.logout().subscribe({
      next: () => {
        this.api.setActiveTeamId(null);
        this.auth = { isAuthenticated: false, googleConfigured: Boolean(this.auth?.googleConfigured) };
        this.teams = [];
        this.activeTeamId = null;
        this.toast.success('Signed out.');
      }
    });
  }

  createTeam(): void {
    this.api.createTeam(this.teamName).subscribe({
      next: team => {
        this.teams = [...this.teams, team];
        this.selectTeam(team);
        this.toast.success('Team created.');
      },
      error: () => this.toast.error('Could not create team.')
    });
  }

  joinTeam(): void {
    this.api.joinTeam(this.inviteCode).subscribe({
      next: joinedTeam => {
        this.teams = this.teams.map(team => (team.id === joinedTeam.id ? joinedTeam : team));
        if (!this.teams.some(team => team.id === joinedTeam.id)) {
          this.teams = [...this.teams, joinedTeam];
        }
        this.selectTeam(joinedTeam);
        this.toast.success('Joined team.');
      },
      error: () => this.toast.error('Invite code was not found.')
    });
  }

  selectTeam(team: Team): void {
    this.activeTeamId = team.id;
    this.api.setActiveTeamId(team.id);
  }

  openWorkspace(): void {
    if (!this.activeTeamId) {
      return;
    }

    this.enterWorkspace();
  }

  copyInvite(team: Team): void {
    navigator.clipboard
      .writeText(team.inviteCode)
      .then(() => this.toast.success('Invite code copied.'))
      .catch(() => this.toast.error('Could not copy invite code.'));
  }

  private resolveActiveTeam(teams: Team[]): number | null {
    if (!teams.length) {
      this.api.setActiveTeamId(null);
      return null;
    }

    const existing = this.api.getActiveTeamId();
    const active = teams.some(team => team.id === existing) ? existing : teams[0].id;
    this.api.setActiveTeamId(active);
    return active;
  }

  private enterWorkspace(): void {
    this.isEntering = true;
    window.setTimeout(() => this.router.navigateByUrl('/'), 420);
  }
}
