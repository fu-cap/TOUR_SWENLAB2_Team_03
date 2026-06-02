import { Routes } from '@angular/router';
import { HomeComponent } from './pages/home/home.component/home.component';
import { LoginComponent } from './pages/login/login.component/login.component';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'home', component: HomeComponent },
  { path: '**', redirectTo: 'login' }
];
