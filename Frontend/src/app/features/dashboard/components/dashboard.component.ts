import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PolicyResponse } from '../../../shared/models/response/policy-response.interface';
import { RoomResponse } from '../../../shared/models/response/room-response.interface';
import { AuthService } from '../../../shared/services/auth.service';
import { PolicyApiService } from '../../../shared/services/policy-api.service';
import { RoomApiService } from '../../../shared/services/room-api.service';
import { TelemetryService } from '../../../shared/services/telemetry.service';
import { RoomComponent } from '../../room/components/room.component';

const MaxNameLength = 128;

@Component({
  selector: 'app-dashboard',
  imports: [ReactiveFormsModule, RouterLink, RoomComponent],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit, OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly roomApiService = inject(RoomApiService);
  private readonly telemetryService = inject(TelemetryService);
  private readonly policyApiService = inject(PolicyApiService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly rooms = signal<RoomResponse[]>([]);
  protected readonly policies = signal<PolicyResponse[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly roomForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(MaxNameLength)]]
  });

  ngOnInit(): void {
    // Signed-out visitors only see the sign in and register links.
    if (!this.isSignedIn()) {
      return;
    }

    this.load();
    this.telemetryService.connect();
  }

  ngOnDestroy(): void {
    this.telemetryService.disconnect();
  }

  protected isSignedIn(): boolean {
    return this.authService.isSignedIn();
  }

  protected name(): string | null {
    return this.authService.name();
  }

  /** Signs out and drops the account's data, so the next account starts clean. */
  protected signOut(): void {
    this.authService.signOut();
    this.telemetryService.disconnect();

    this.rooms.set([]);
    this.policies.set([]);
    this.errorMessage.set(null);
    this.isLoading.set(true);
  }

  protected createRoom(): void {
    if (this.roomForm.invalid) {
      return;
    }

    this.roomApiService.CreateRoom({ name: this.roomForm.controls.name.value.trim() }).subscribe({
      next: (room) => {
        this.rooms.update(rooms => [...rooms, room]);
        this.roomForm.reset();
        this.errorMessage.set(null);
      },
      error: (error: HttpErrorResponse) => this.showError(error, 'Could not create the room.')
    });
  }

  /** Swap in a room the room card changed. */
  protected replaceRoom(updated: RoomResponse): void {
    this.rooms.update(rooms => rooms.map(x => (x.id === updated.id) ? updated : x));
  }

  /** Drop a room the room card deleted. */
  protected removeRoom(roomId: string): void {
    this.rooms.update(rooms => rooms.filter(x => x.id !== roomId));
  }

  private load(): void {
    // Needed so an actuator driven by a policy can show its override switch.
    this.policyApiService.GetPolicies().subscribe({
      next: (policies) => this.policies.set(policies),
      error: (error: HttpErrorResponse) => this.showError(error, 'Could not load your policies.')
    });

    this.roomApiService.GetRooms().subscribe({
      next: (rooms) => {
        this.rooms.set(rooms);
        this.isLoading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.isLoading.set(false);
        this.showError(error, 'Could not load your rooms.');
      }
    });
  }

  private showError(error: HttpErrorResponse, fallback: string): void {
    this.errorMessage.set(typeof error.error === 'string' ? error.error : fallback);
  }
}
