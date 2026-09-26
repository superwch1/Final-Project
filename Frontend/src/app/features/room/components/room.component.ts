import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, input, model, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PolicyResponse } from '../../../shared/models/response/policy-response.interface';
import { RoomResponse } from '../../../shared/models/response/room-response.interface';
import { RoomApiService } from '../../../shared/services/room-api.service';
import { DeviceComponent } from '../../device/components/device.component';

const MacAddressPattern = /^\s*(?:[0-9A-Fa-f]{2}[:-]?){5}[0-9A-Fa-f]{2}\s*$/;
const MaxNameLength = 128;

@Component({
  selector: 'app-room',
  imports: [ReactiveFormsModule, DeviceComponent],
  templateUrl: './room.component.html'
})
export class RoomComponent {
  private readonly roomApiService = inject(RoomApiService);
  private readonly formBuilder = inject(FormBuilder);

  /** The room this card shows. */
  readonly room = input.required<RoomResponse>();

  /** Every room the user owns, so a sensor can drive an actuator in another room. */
  readonly rooms = input.required<RoomResponse[]>();

  /** Every policy the user owns, shared with the other rooms. */
  readonly policies = model.required<PolicyResponse[]>();

  /** Raised after the room is renamed or a device is paired or unpaired. */
  readonly roomUpdated = output<RoomResponse>();

  /** Raised with the room ID after the room is deleted. */
  readonly roomDeleted = output<string>();

  /** Raised with an error to show, or null to clear it. */
  readonly errorMessageChange = output<string | null>();

  protected readonly deviceForm = this.formBuilder.nonNullable.group({
    macAddress: ['', [Validators.required, Validators.pattern(MacAddressPattern)]],
    name: ['', [Validators.required, Validators.maxLength(MaxNameLength)]]
  });

  protected renameRoom(): void {
    const room = this.room();
    const name = prompt('New name', room.name)?.trim();

    if (!name) {
      return;
    }

    this.roomApiService.RenameRoom(room.id, { name }).subscribe({
      next: (updated) => {
        this.roomUpdated.emit(updated);
        this.errorMessageChange.emit(null);
      },
      error: (error: HttpErrorResponse) => this.showError(error, 'Could not rename the room.')
    });
  }

  protected deleteRoom(): void {
    const room = this.room();

    if (!confirm(`Delete "${room.name}" and unpair its devices?`)) {
      return;
    }

    this.roomApiService.DeleteRoom(room.id).subscribe({
      next: () => {
        this.roomDeleted.emit(room.id);
        this.errorMessageChange.emit(null);
      },
      error: (error: HttpErrorResponse) => this.showError(error, 'Could not delete the room.')
    });
  }

  protected pairDevice(): void {
    if (this.deviceForm.invalid) {
      return;
    }

    const request = {
      macAddress: this.deviceForm.controls.macAddress.value.trim(),
      name: this.deviceForm.controls.name.value.trim()
    };

    this.roomApiService.PairDevice(this.room().id, request).subscribe({
      next: (device) => {
        this.roomUpdated.emit({ ...this.room(), devices: [...this.room().devices, device] });
        this.deviceForm.reset();
        this.errorMessageChange.emit(null);
      },
      error: (error: HttpErrorResponse) => this.showError(error, 'Could not pair the device.')
    });
  }

  /** Unpairs a device after its row has confirmed with the user. */
  protected unpairDevice(macAddress: string): void {
    this.roomApiService.UnpairDevice(this.room().id, macAddress).subscribe({
      next: () => {
        this.roomUpdated.emit({
          ...this.room(),
          devices: this.room().devices.filter(device => device.macAddress !== macAddress)
        });

        this.errorMessageChange.emit(null);
      },
      error: (error: HttpErrorResponse) => this.showError(error, 'Could not unpair the device.')
    });
  }

  private showError(error: HttpErrorResponse, fallback: string): void {
    this.errorMessageChange.emit(typeof error.error === 'string' ? error.error : fallback);
  }
}
