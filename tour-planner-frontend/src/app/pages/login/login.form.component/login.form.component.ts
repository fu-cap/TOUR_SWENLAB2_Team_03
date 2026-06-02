import { Component, Output, EventEmitter, inject } from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormControl, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ZardIdDirective } from '@/shared/core';
import { ZardButtonComponent } from '@/shared/components/button';
import { ZardInputImports } from '@/shared/components/input';
import { ZardFormImports } from '@/shared/components/form';

@Component({
  selector: 'app-login-form',
  imports: [ReactiveFormsModule, ZardButtonComponent, ...ZardInputImports, ...ZardFormImports, ZardIdDirective],
  templateUrl: './login.form.component.html',
  styleUrl: './login.form.component.css',
})
export class LoginFormComponent {
  @Output() loginComponentStatus = new EventEmitter<string>();
  private router = inject(Router);

  form = new FormGroup({
    username: new FormControl('', Validators.required),
    password: new FormControl('', Validators.required),
  })

  public onSubmit() {
    // For now, just navigate to home
    this.router.navigate(['/home']);
  }

  public changeStatus() {
    this.loginComponentStatus.emit('register');
  }
}

