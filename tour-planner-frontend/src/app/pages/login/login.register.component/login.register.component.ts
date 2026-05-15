import { Component, Output, EventEmitter } from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormControl, Validators } from '@angular/forms';
import { ZardIdDirective } from '@/shared/core';
import { ZardButtonComponent } from '@/shared/components/button';
import { ZardInputDirective } from '@/shared/components/input';
import { ZardFormImports } from '@/shared/components/form';
import { ZardSelectImports } from '@/shared/components/select';

@Component({
  selector: 'app-register-form',
  imports: [ReactiveFormsModule, ZardButtonComponent, ZardInputDirective, ZardFormImports,
    ZardIdDirective, ZardSelectImports],
  templateUrl: './login.register.component.html',
  styleUrl: './login.register.component.css',
})
export class LoginRegisterComponent {
  @Output() loginComponentStatus = new EventEmitter<string>();

  form = new FormGroup({
    gender: new FormControl('', Validators.required),
    firstname: new FormControl('', Validators.required),
    lastname: new FormControl('', Validators.required),
    email: new FormControl('', [Validators.required, Validators.email]),
    username: new FormControl('', [Validators.required, Validators.pattern(/^[a-zA-Z0-9_]+$/)]),
    password: new FormControl('', [Validators.required, Validators.minLength(9)]),
  })

  public changeStatus() {
    this.loginComponentStatus.emit('login');
  }
}
