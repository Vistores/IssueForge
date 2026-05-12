import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthStatus } from '../../core/models/api.models';
import { Api } from '../../core/services/api';
import { Toast } from '../../core/services/toast';

@Component({
  selector: 'app-account',
  imports: [FormsModule],
  templateUrl: './account.html'
})
export class AccountPage implements OnInit {
  auth?: AuthStatus;
  avatarUrl = '';
  isLoading = true;

  constructor(
    private readonly api: Api,
    private readonly router: Router,
    private readonly toast: Toast
  ) {}

  ngOnInit(): void {
    this.api.getAuthStatus().subscribe({
      next: auth => {
        this.auth = auth;
        this.avatarUrl = auth.avatarUrl ?? '';
        this.isLoading = false;
      },
      error: () => (this.isLoading = false)
    });
  }

  saveAvatar(): void {
    this.api.updateAccount({ avatarUrl: this.avatarUrl }).subscribe({
      next: auth => {
        this.auth = auth;
        this.avatarUrl = auth.avatarUrl ?? '';
        this.toast.success('Profile photo updated.');
      },
      error: () => this.toast.error('Could not update profile photo.')
    });
  }

  logout(): void {
    this.api.logout().subscribe({
      next: () => {
        this.api.setActiveTeamId(null);
        this.toast.success('Signed out.');
        this.router.navigateByUrl('/auth');
      }
    });
  }
}
