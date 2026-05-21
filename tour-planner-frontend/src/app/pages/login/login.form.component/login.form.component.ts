import { Component, Output, EventEmitter } from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormControl, Validators } from '@angular/forms';
import { ZardIdDirective } from '@/shared/core';
import { ZardButtonComponent } from '@/shared/components/button';
import { ZardInputDirective } from '@/shared/components/input';
import { ZardFormImports } from '@/shared/components/form';

@Component({
  selector: 'app-login-form',
  imports: [ReactiveFormsModule, ZardButtonComponent, ZardInputDirective, ZardFormImports, ZardIdDirective],
  templateUrl: './login.form.component.html',
  styleUrl: './login.form.component.css',
})
export class LoginFormComponent {
  @Output() loginComponentStatus = new EventEmitter<string>();

  form = new FormGroup({
    username: new FormControl('', Validators.required),
    password: new FormControl('', Validators.required),
  })

  public changeStatus() {
    this.loginComponentStatus.emit('register');
  }
}

