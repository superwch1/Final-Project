import { Routes } from '@angular/router';
import { HomeComponent } from './features/account/components/home/home.component';
import { RoomComponent } from './features/dashboard/components/room/room.component';

export const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', component: HomeComponent },
  { path: 'room', component: RoomComponent }
];
