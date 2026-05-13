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
  pendingAvatarDataUrl = '';
  avatarScale = 1;
  avatarOffsetX = 0;
  avatarOffsetY = 0;
  isDeleteAccountModalOpen = false;

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

  removeAvatar(): void {
    this.avatarUrl = '';
    this.saveAvatar();
  }

  get cropTransform(): string {
    return `translate(${this.avatarOffsetX}px, ${this.avatarOffsetY}px) scale(${this.avatarScale})`;
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
    input.value = '';
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

  openDeleteAccountModal(): void {
    this.isDeleteAccountModalOpen = true;
  }

  closeDeleteAccountModal(): void {
    this.isDeleteAccountModalOpen = false;
  }

  deleteAccount(): void {
    this.api.deleteAccount().subscribe({
      next: () => {
        this.api.setActiveTeamId(null);
        this.toast.success('Account deleted.');
        this.router.navigateByUrl('/auth');
      },
      error: error => {
        const message = error?.error?.message ?? 'Could not delete account.';
        this.toast.error(message);
      }
    });
  }

  closeAvatarCrop(): void {
    this.pendingAvatarDataUrl = '';
  }

  saveCroppedAvatar(): void {
    if (!this.pendingAvatarDataUrl) {
      return;
    }

    const image = new Image();
    image.onload = () => {
      const size = 192;
      const canvas = document.createElement('canvas');
      canvas.width = size;
      canvas.height = size;
      const context = canvas.getContext('2d');
      if (!context) {
        this.toast.error('Could not prepare avatar image.');
        return;
      }

      context.fillStyle = '#f3dfb4';
      context.fillRect(0, 0, size, size);
      context.save();
      context.beginPath();
      context.arc(size / 2, size / 2, size / 2, 0, Math.PI * 2);
      context.clip();

      const baseScale = Math.max(size / image.width, size / image.height);
      const drawWidth = image.width * baseScale * this.avatarScale;
      const drawHeight = image.height * baseScale * this.avatarScale;
      const x = (size - drawWidth) / 2 + this.avatarOffsetX * 2;
      const y = (size - drawHeight) / 2 + this.avatarOffsetY * 2;
      context.drawImage(image, x, y, drawWidth, drawHeight);
      context.restore();

      this.avatarUrl = canvas.toDataURL('image/jpeg', 0.86);
      this.pendingAvatarDataUrl = '';
      this.saveAvatar();
    };
    image.onerror = () => this.toast.error('Could not read avatar image.');
    image.src = this.pendingAvatarDataUrl;
  }

  private readAvatarFile(file: File): void {
    if (!file.type.startsWith('image/')) {
      this.toast.error('Please choose an image file.');
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      this.pendingAvatarDataUrl = String(reader.result ?? '');
      this.avatarScale = 1;
      this.avatarOffsetX = 0;
      this.avatarOffsetY = 0;
    };
    reader.readAsDataURL(file);
  }
}
