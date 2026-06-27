import { Component, Output, EventEmitter, inject } from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormControl, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ZardIdDirective } from '@/shared/core';
import { ZardButtonComponent } from '@/shared/components/button';
import { ZardInputImports } from '@/shared/components/input';
import { ZardFormImports } from '@/shared/components/form';
import { AuthService } from '@/shared/core/services/auth.service';
import { toast } from 'ngx-sonner';

@Component({
  selector: 'app-login-form',
  imports: [ReactiveFormsModule, ZardButtonComponent, ...ZardInputImports, ...ZardFormImports, ZardIdDirective],
  templateUrl: './login.form.component.html',
  styleUrl: './login.form.component.css',
})
export class LoginFormComponent {
  @Output() loginComponentStatus = new EventEmitter<string>();
  private router = inject(Router);
  private authService = inject(AuthService);

  showPassword = false;

  form = new FormGroup({
    username: new FormControl('', Validators.required),
    password: new FormControl('', Validators.required),
  })

  public onSubmit() {
    if (this.form.invalid) return;

    const { username, password } = this.form.value;

    if (!username || !password) {
      toast.error('Please enter username and password.');
      return;
    }

    this.authService.login({ username, password }).subscribe({
      next: (user) => {
        toast.success(`Welcome back, ${user.username}!`);
        this.router.navigate(['/home']);
      },
      error: (err) => {
        console.error('Login failed:', err);
        const errMsg = err.error?.message || 'Invalid username or password.';
        toast.error(errMsg);
      }
    });
  }

  public changeStatus() {
    this.loginComponentStatus.emit('register');
  }
}

