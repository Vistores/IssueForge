import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthStatus, Team } from '../../core/models/api.models';
import { Api } from '../../core/services/api';
import { Toast } from '../../core/services/toast';

@Component({
  selector: 'app-auth',
  imports: [FormsModule],
  templateUrl: './auth.html'
})
export class AuthPage implements OnInit {
  auth?: AuthStatus;
  inviteCode = '';
  loginEmail = '';
  loginPassword = '';
  registerName = '';
  registerEmail = '';
  registerPassword = '';
  authMode: 'login' | 'register' = 'login';
  isEntering = false;
  isLoading = true;

  constructor(
    private readonly api: Api,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly toast: Toast
  ) {}

  ngOnInit(): void {
    this.inviteCode = this.route.snapshot.queryParamMap.get('inviteCode') ?? '';
    this.api.getAuthStatus().subscribe({
      next: auth => {
        this.auth = auth;
        this.isLoading = false;
        if (auth.isAuthenticated) {
          this.continueAfterAuth();
        }
      },
      error: () => (this.isLoading = false)
    });
  }

  login(): void {
    this.api.login({ email: this.loginEmail, password: this.loginPassword }).subscribe({
      next: auth => {
        this.auth = auth;
        this.toast.success('Signed in.');
        this.continueAfterAuth();
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
          this.continueAfterAuth();
        },
        error: () => this.toast.error('Could not create account.')
      });
  }

  loginWithGoogle(): void {
    window.location.href = this.api.getGoogleLoginUrl(this.inviteCode || undefined);
  }

  private continueAfterAuth(): void {
    if (this.inviteCode) {
      this.api.joinTeam(this.inviteCode).subscribe({
        next: team => {
          this.api.setActiveTeamId(team.id);
          this.toast.success(`Joined ${team.name}.`);
          this.enter('/');
        },
        error: () => {
          this.toast.error('Invite code was not found.');
          this.enter('/teams');
        }
      });
      return;
    }

    this.api.getTeams().subscribe({
      next: teams => {
        this.setFirstTeam(teams);
        this.enter(teams.length ? '/' : '/teams');
      },
      error: () => this.enter('/teams')
    });
  }

  private setFirstTeam(teams: Team[]): void {
    const activeTeamId = this.api.getActiveTeamId();
    if (!teams.some(team => team.id === activeTeamId)) {
      this.api.setActiveTeamId(teams[0]?.id ?? null);
    }
  }

  private enter(url: string): void {
    this.isEntering = true;
    window.setTimeout(() => this.router.navigateByUrl(url), 420);
  }
}
