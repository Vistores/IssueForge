import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthStatus } from '../../core/models/api.models';
import { Api } from '../../core/services/api';
import { Toast } from '../../core/services/toast';

@Component({
  selector: 'app-account',
  templateUrl: './account.html'
})
export class AccountPage implements OnInit {
  auth?: AuthStatus;
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
        this.isLoading = false;
      },
      error: () => (this.isLoading = false)
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
