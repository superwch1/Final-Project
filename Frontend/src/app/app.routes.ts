import { Routes } from '@angular/router';
import { HomeComponent } from './features/account/components/home/home.component';
import { LoginComponent } from './features/account/components/login/login.component';
import { RegisterComponent } from './features/account/components/register/register.component';
import { RoomListComponent } from './features/room/components/room-list/room-list.component';

export const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', component: HomeComponent },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'rooms', component: RoomListComponent }
];
