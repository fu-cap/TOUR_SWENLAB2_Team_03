import { Routes } from '@angular/router';
import { HomeComponent } from './pages/home.component/home.component';
import { LoginComponent } from './pages/login.component/login.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'home', component: HomeComponent },
];
