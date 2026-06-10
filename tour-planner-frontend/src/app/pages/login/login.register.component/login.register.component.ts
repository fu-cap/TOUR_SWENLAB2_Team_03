import { Component, Output, EventEmitter, inject } from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormControl, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ZardIdDirective } from '@/shared/core';
import { ZardButtonComponent } from '@/shared/components/button';
import { ZardInputImports } from '@/shared/components/input';
import { ZardFormImports } from '@/shared/components/form';
import { ZardSelectImports } from '@/shared/components/select';
import { AuthService } from '@/shared/core/services/auth.service';
import { toast } from 'ngx-sonner';

@Component({
  selector: 'app-register-form',
  imports: [ReactiveFormsModule, ZardButtonComponent, ...ZardInputImports, ...ZardFormImports,
    ZardIdDirective, ZardSelectImports],
  templateUrl: './login.register.component.html',
  styleUrl: './login.register.component.css',
})
export class LoginRegisterComponent {
  @Output() loginComponentStatus = new EventEmitter<string>();
  private router = inject(Router);
  private authService = inject(AuthService);

  form = new FormGroup({
    gender: new FormControl('', Validators.required),
    firstname: new FormControl('', Validators.required),
    lastname: new FormControl('', Validators.required),
    email: new FormControl('', [Validators.required, Validators.email]),
    username: new FormControl('', [Validators.required, Validators.pattern(/^[a-zA-Z0-9_]+$/)]),
    password: new FormControl('', [Validators.required, Validators.minLength(9)]),
  })

  public onSubmit() {
    if (this.form.invalid) return;

    const { username, email, password, gender, firstname, lastname } = this.form.value;

    if (!username || !email || !password || !gender || !firstname || !lastname) {
      toast.error('Please fill out all required fields.');
      return;
    }

    this.authService.register({ username, email, password, gender, firstname, lastname }).subscribe({
      next: () => {
        toast.success('Registration successful! Please log in.');
        this.loginComponentStatus.emit('login');
      },
      error: (err) => {
        console.error('Registration failed:', err);
        const errMsg = err.error?.message || 'Registration failed. Please try again.';
        toast.error(errMsg);
      }
    });
  }

  public changeStatus() {
    this.loginComponentStatus.emit('login');
  }
}
