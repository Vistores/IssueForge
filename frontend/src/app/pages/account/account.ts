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
  isDragOver = false;

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

  triggerFile(input: HTMLInputElement): void {
    input.click();
  }

  handleFileInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) {
      this.readAvatarFile(file);
    }
  }

  handleDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = false;
    const file = event.dataTransfer?.files?.[0];
    if (file) {
      this.readAvatarFile(file);
    }
  }

  handlePaste(event: ClipboardEvent): void {
    const item = Array.from(event.clipboardData?.items ?? []).find(item => item.type.startsWith('image/'));
    const file = item?.getAsFile();
    if (file) {
      this.readAvatarFile(file);
    }
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

  private readAvatarFile(file: File): void {
    if (!file.type.startsWith('image/')) {
      this.toast.error('Please choose an image file.');
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      this.avatarUrl = String(reader.result ?? '');
      this.saveAvatar();
    };
    reader.readAsDataURL(file);
  }
}
