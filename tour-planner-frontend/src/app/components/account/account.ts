import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, UpdateUserRequest } from '@/shared/core/services/auth.service';
import { ZardButtonComponent } from '@/shared/components/button';
import { ZardInputImports } from '@/shared/components/input';
import { ZardSelectImports } from '@/shared/components/select';

@Component({
  selector: 'app-account',
  standalone: true,
  imports: [CommonModule, FormsModule, ZardButtonComponent, ...ZardInputImports, ZardSelectImports],
  templateUrl: './account.html',
  styleUrl: './account.css',
})
export class AccountComponent {
  public authService = inject(AuthService);
  private router = inject(Router);

  editMode = signal(false);
  showDeleteConfirm = signal(false);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);

  editData: UpdateUserRequest = {
    username: '',
    email: '',
    password: '',
    gender: '',
    firstname: '',
    lastname: '',
  };

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  enterEditMode() {
    const user = this.authService.currentUser();
    if (!user) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.getUserById(user.id).subscribe({
      next: (fullProfile) => {
        this.editData = {
          username: fullProfile.username,
          email: fullProfile.email,
          password: '',
          gender: fullProfile.gender,
          firstname: fullProfile.firstName,
          lastname: fullProfile.lastName,
        };
        this.isLoading.set(false);
        this.editMode.set(true);
      },
      error: () => {
        this.errorMessage.set('Failed to load profile data.');
        this.isLoading.set(false);
      },
    });
  }

  cancelEdit() {
    this.editMode.set(false);
    this.errorMessage.set(null);
  }

  saveChanges() {
    const user = this.authService.currentUser();
    if (!user) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.updateUser(user.id, this.editData).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.editMode.set(false);
      },
      error: (err) => {
        const msg = err?.error?.message ?? 'Failed to save changes.';
        this.errorMessage.set(msg);
        this.isLoading.set(false);
      },
    });
  }

  requestDelete() {
    this.showDeleteConfirm.set(true);
  }

  cancelDelete() {
    this.showDeleteConfirm.set(false);
  }

  confirmDelete() {
    const user = this.authService.currentUser();
    if (!user) return;

    this.isLoading.set(true);

    this.authService.deleteUser(user.id).subscribe({
      next: () => {
        this.authService.logout();
        this.router.navigate(['/login']);
      },
      error: () => {
        this.errorMessage.set('Failed to delete account.');
        this.isLoading.set(false);
        this.showDeleteConfirm.set(false);
      },
    });
  }
}
