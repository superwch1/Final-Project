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

  /** Load rooms and policies when signed in */
  ngOnInit(): void {
    if (!this.isSignedIn()) {
      return;
    }

    this.load();
    this.telemetryService.connect();
  }

  /** Stop live telemetry when the page closes */
  ngOnDestroy(): void {
    this.telemetryService.disconnect();
  }

  /** Returns true if the user has a valid access token */
  protected isSignedIn(): boolean {
    return this.authService.isSignedIn();
  }

  /** Returns the user's name */
  protected name(): string | null {
    return this.authService.name();
  }

  /** Signs out */
  protected signOut(): void {
    this.authService.signOut();
    this.telemetryService.disconnect();

    this.rooms.set([]);
    this.policies.set([]);
    this.errorMessage.set(null);
    this.isLoading.set(true);
  }

  /** Creates a room */
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

  /** Update a room */
  protected updateRoom(updated: RoomResponse): void {
    this.rooms.update(rooms => rooms.map(x => (x.id === updated.id) ? updated : x));
  }

  /** Removes a room */
  protected removeRoom(roomId: string): void {
    this.rooms.update(rooms => rooms.filter(x => x.id !== roomId));
  }

  /** Loads the user's policies and rooms */
  private load(): void {
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

  /** Shows the error message */
  private showError(error: HttpErrorResponse, fallback: string): void {
    this.errorMessage.set(typeof error.error === 'string' ? error.error : fallback);
  }
}
