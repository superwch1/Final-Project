import { Routes } from '@angular/router';
import { LoginComponent } from './features/login/components/login.component';
import { RegisterComponent } from './features/register/components/register.component';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent }
];
