import { Routes } from '@angular/router';
import { HomeComponent } from './pages/home/home.component/home.component';
import { LoginComponent } from './pages/login/login.component/login.component';
import { authGuard } from './shared/core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'home', component: HomeComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: 'login' }
];
