import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ActuatorState } from '../../../dashboard/enumerations/actuator-state.enum';
import { DeviceType } from '../../../dashboard/enumerations/device-type.enum';
import { BaseTelemetry } from '../../../dashboard/models/telemetry/base-telemetry.interface';
import { LedTelemetry } from '../../../dashboard/models/telemetry/led-telemetry.interface';
import { LightTelemetry } from '../../../dashboard/models/telemetry/light-telemetry.interface';
import { TempAndHumidTelemetry } from '../../../dashboard/models/telemetry/temp-and-humid-telemetry.interface';
import { DeviceApiService } from '../../../dashboard/services/device-api.service';
import { RoomResponse } from '../../models/room-response.interface';
import { RoomApiService } from '../../services/room-api.service';
import { TelemetryService } from '../../services/telemetry.service';

const MacAddressPattern = /^\s*(?:[0-9A-Fa-f]{2}[:-]?){5}[0-9A-Fa-f]{2}\s*$/;
const MaxNameLength = 128;

@Component({
  selector: 'app-room-list',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './room-list.component.html'
})
export class RoomListComponent implements OnInit, OnDestroy {
  private readonly roomApiService = inject(RoomApiService);
  private readonly deviceApiService = inject(DeviceApiService);
  private readonly telemetryService = inject(TelemetryService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly telemetry = this.telemetryService.telemetry;
  protected readonly ActuatorState = ActuatorState;
  private readonly deviceForms = new Map<string, FormGroup>();

  protected readonly rooms = signal<RoomResponse[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly roomForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(MaxNameLength)]]
  });

  ngOnInit(): void {
    this.load();
    this.telemetryService.connect();
  }

  ngOnDestroy(): void {
    this.telemetryService.disconnect();
  }

  /** Groups a stored MAC into pairs for display: 98CDAC261D12 -> 98:CD:AC:26:1D:12 */
  protected formatMacAddress(macAddress: string): string {
    return macAddress.match(/.{1,2}/g)?.join(':') ?? macAddress;
  }

  protected telemetryFor(macAddress: string): BaseTelemetry | undefined {
    return this.telemetry()[TelemetryService.normalizeMacAddress(macAddress)];
  }

  protected isActuator(telemetry: BaseTelemetry): boolean {
    return (telemetry.deviceType === DeviceType.LedActuator) || (telemetry.deviceType === DeviceType.FanActuator);
  }

  protected actuatorState(telemetry: BaseTelemetry): ActuatorState {
    return (telemetry as LedTelemetry).actuatorState;
  }

  protected reading(telemetry: BaseTelemetry): string {
    if (telemetry.deviceType === DeviceType.TempAndHumidSensor) {
      const sensor = telemetry as TempAndHumidTelemetry;
      return `${sensor.temperatureReading} °C, ${sensor.humidityReading} % humidity`;
    }

    if (telemetry.deviceType === DeviceType.LightSensor) {
      return `${(telemetry as LightTelemetry).lightReading} % light`;
    }

    return telemetry.deviceType;
  }

  protected toggleActuator(macAddress: string, telemetry: BaseTelemetry): void {
    const actuatorState = (this.actuatorState(telemetry) === ActuatorState.On) ? ActuatorState.Off : ActuatorState.On;

    this.deviceApiService.SetActuatorState({ macAddress, actuatorState }).subscribe({
      error: (error: HttpErrorResponse) => this.showError(error, 'Could not switch the device.')
    });
  }

  protected deviceFormFor(roomId: string): FormGroup {
    let form = this.deviceForms.get(roomId);

    if (form === undefined) {
      form = this.formBuilder.nonNullable.group({
        macAddress: ['', [Validators.required, Validators.pattern(MacAddressPattern)]],
        name: ['', [Validators.required, Validators.maxLength(MaxNameLength)]]
      });

      this.deviceForms.set(roomId, form);
    }

    return form;
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
      macAddress: form.controls['macAddress'].value.trim(),
      name: form.controls['name'].value.trim()
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
    if (!confirm(`Unpair ${this.formatMacAddress(macAddress)}?`)) {
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
