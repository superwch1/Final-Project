import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { RoomResponse } from '../../models/room-response.interface';
import { RoomApiService } from '../../services/room-api.service';

@Component({
  selector: 'app-room-list',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './room-list.component.html'
})
export class RoomListComponent implements OnInit {
  private readonly roomApiService = inject(RoomApiService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly deviceForms = new Map<string, FormGroup>();

  protected readonly rooms = signal<RoomResponse[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly roomForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required]]
  });

  ngOnInit(): void {
    this.load();
  }

  protected deviceFormFor(roomId: string): FormGroup {
    let form = this.deviceForms.get(roomId);

    if (form === undefined) {
      form = this.formBuilder.nonNullable.group({
        macAddress: ['', [Validators.required]],
        name: ['', [Validators.required]]
      });

      this.deviceForms.set(roomId, form);
    }

    return form;
  }

  protected createRoom(): void {
    if (this.roomForm.invalid) {
      return;
    }

    this.roomApiService.CreateRoom({ name: this.roomForm.controls.name.value }).subscribe({
      next: (room) => {
        this.rooms.update(rooms => [...rooms, room]);
        this.roomForm.reset();
        this.errorMessage.set(null);
      },
      error: (error: HttpErrorResponse) => this.showError(error, 'Could not create the room.')
    });
  }

  protected renameRoom(room: RoomResponse): void {
    const name = prompt('New name', room.name)?.trim();

    if (!name) {
      return;
    }

    this.roomApiService.RenameRoom(room.id, { name }).subscribe({
      next: (updated) => {
        this.rooms.update(rooms => rooms.map(x => (x.id === updated.id) ? updated : x));
        this.errorMessage.set(null);
      },
      error: (error: HttpErrorResponse) => this.showError(error, 'Could not rename the room.')
    });
  }

  protected deleteRoom(room: RoomResponse): void {
    if (!confirm(`Delete "${room.name}" and unpair its devices?`)) {
      return;
    }

    this.roomApiService.DeleteRoom(room.id).subscribe({
      next: () => {
        this.rooms.update(rooms => rooms.filter(x => x.id !== room.id));
        this.deviceForms.delete(room.id);
        this.errorMessage.set(null);
      },
      error: (error: HttpErrorResponse) => this.showError(error, 'Could not delete the room.')
    });
  }

  protected pairDevice(roomId: string): void {
    const form = this.deviceFormFor(roomId);

    if (form.invalid) {
      return;
    }

    const request = {
      macAddress: form.controls['macAddress'].value,
      name: form.controls['name'].value
    };

    this.roomApiService.PairDevice(roomId, request).subscribe({
      next: (device) => {
        this.rooms.update(rooms => rooms.map(room =>
          (room.id === roomId) ? { ...room, devices: [...room.devices, device] } : room));

        form.reset();
        this.errorMessage.set(null);
      },
      error: (error: HttpErrorResponse) => this.showError(error, 'Could not pair the device.')
    });
  }

  protected unpairDevice(roomId: string, macAddress: string): void {
    if (!confirm(`Unpair ${macAddress}?`)) {
      return;
    }

    this.roomApiService.UnpairDevice(roomId, macAddress).subscribe({
      next: () => {
        this.rooms.update(rooms => rooms.map(room =>
          (room.id === roomId)
            ? { ...room, devices: room.devices.filter(device => device.macAddress !== macAddress) }
            : room));

        this.errorMessage.set(null);
      },
      error: (error: HttpErrorResponse) => this.showError(error, 'Could not unpair the device.')
    });
  }

  private load(): void {
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
