import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RoomListComponent } from '../../../room/components/room-list/room-list.component';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-home',
  imports: [RouterLink, RoomListComponent],
  templateUrl: './home.component.html'
})
export class HomeComponent {
  private readonly authService = inject(AuthService);

  protected isSignedIn(): boolean {
    return this.authService.isSignedIn();
  }

  protected name(): string | null {
    return this.authService.name();
  }

  protected signOut(): void {
    this.authService.signOut();
  }
}
