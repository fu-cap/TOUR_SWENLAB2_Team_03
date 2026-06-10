import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '@/shared/core/services/auth.service';
import { ZardButtonComponent } from '@/shared/components/button';

@Component({
  selector: 'app-account',
  standalone: true,
  imports: [CommonModule, ZardButtonComponent],
  templateUrl: './account.html',
  styleUrl: './account.css',
})
export class AccountComponent {
  public authService = inject(AuthService);
  private router = inject(Router);

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
