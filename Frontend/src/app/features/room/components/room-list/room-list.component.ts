import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActuatorState } from '../../../dashboard/enumerations/actuator-state.enum';
import { DeviceType } from '../../../dashboard/enumerations/device-type.enum';
import { BaseTelemetry } from '../../../dashboard/models/telemetry/base-telemetry.interface';
import { LedTelemetry } from '../../../dashboard/models/telemetry/led-telemetry.interface';
import { LightTelemetry } from '../../../dashboard/models/telemetry/light-telemetry.interface';
import { TempAndHumidTelemetry } from '../../../dashboard/models/telemetry/temp-and-humid-telemetry.interface';
import { DeviceApiService } from '../../../dashboard/services/device-api.service';
import { Comparison } from '../../../policy/enumerations/comparison.enum';
import { SensorReading } from '../../../policy/enumerations/sensor-reading.enum';
import { PolicyResponse } from '../../../policy/models/policy-response.interface';
import { PolicyApiService } from '../../../policy/services/policy-api.service';
import { DeviceResponse } from '../../models/device-response.interface';
import { RoomResponse } from '../../models/room-response.interface';
import { RoomApiService } from '../../services/room-api.service';
import { TelemetryService } from '../../services/telemetry.service';

const MacAddressPattern = /^\s*(?:[0-9A-Fa-f]{2}[:-]?){5}[0-9A-Fa-f]{2}\s*$/;
const MaxNameLength = 128;

@Component({
  selector: 'app-room-list',
  imports: [ReactiveFormsModule],
  templateUrl: './room-list.component.html'
})
export class RoomListComponent implements OnInit, OnDestroy {
  private readonly roomApiService = inject(RoomApiService);
  private readonly deviceApiService = inject(DeviceApiService);
  private readonly telemetryService = inject(TelemetryService);
  private readonly policyApiService = inject(PolicyApiService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly telemetry = this.telemetryService.telemetry;
  protected readonly ActuatorState = ActuatorState;
  protected readonly Comparison = Comparison;

  private readonly deviceForms = new Map<string, FormGroup>();

  // One automation form per sensor, so the template gets the same instance back.
  private readonly policyForms = new Map<string, FormGroup>();

  protected readonly rooms = signal<RoomResponse[]>([]);
  protected readonly policies = signal<PolicyResponse[]>([]);
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

  /** The policy driving this actuator, if one does. */
  protected policyFor(macAddress: string): PolicyResponse | undefined {
    return this.policies().find(policy => policy.actuatorMacAddress === macAddress);
  }

  /** Hand control to the policy, or take it back. */
  protected toggleAutomatic(policy: PolicyResponse): void {
    const request = {
      reading: policy.reading,
      comparison: policy.comparison,
      threshold: policy.threshold,
      actuatorState: policy.actuatorState,
      isEnabled: !policy.isEnabled
    };

    this.policyApiService.UpdatePolicy(policy.id, request).subscribe({
      next: (updated) => {
        this.policies.update(policies => policies.map(x => (x.id === updated.id) ? updated : x));
        this.errorMessage.set(null);
      },
      error: (error: HttpErrorResponse) => this.showError(error, 'Could not change the policy.')
    });
  }

  protected isSensor(device: DeviceResponse): boolean {
    return (device.deviceType === DeviceType.LightSensor) || (device.deviceType === DeviceType.TempAndHumidSensor);
  }

  protected isActuatorDevice(device: DeviceResponse): boolean {
    return (device.deviceType === DeviceType.LedActuator) || (device.deviceType === DeviceType.FanActuator);
  }

  /** The readings this sensor reports, so a light sensor cannot watch humidity. */
  protected readingsFor(device: DeviceResponse): SensorReading[] {
    if (device.deviceType === DeviceType.LightSensor) {
      return [SensorReading.Light];
    }

    if (device.deviceType === DeviceType.TempAndHumidSensor) {
      return [SensorReading.Temperature, SensorReading.Humidity];
    }

    return [];
  }

  /** Actuators in any of the user's rooms that no policy drives yet. */
  protected availableActuators(): { device: DeviceResponse; roomName: string }[] {
    const driven = new Set(this.policies().map(policy => policy.actuatorMacAddress));

    return this.rooms()
      .flatMap(room => room.devices.map(device => ({ device, roomName: room.name })))
      .filter(entry => this.isActuatorDevice(entry.device) && !driven.has(entry.device.macAddress));
  }

  protected policyFormFor(sensorMacAddress: string, device: DeviceResponse): FormGroup {
    let form = this.policyForms.get(sensorMacAddress);

    if (form === undefined) {
      form = this.formBuilder.nonNullable.group({
        reading: [this.readingsFor(device)[0] ?? SensorReading.Temperature, [Validators.required]],
        comparison: [Comparison.Above, [Validators.required]],
        threshold: [0, [Validators.required]],
        actuatorMacAddress: ['', [Validators.required]],
        actuatorState: [ActuatorState.On, [Validators.required]]
      });

      this.policyForms.set(sensorMacAddress, form);
    }

    return form;
  }

  protected createPolicy(sensorMacAddress: string, device: DeviceResponse): void {
    const form = this.policyFormFor(sensorMacAddress, device);

    if (form.invalid) {
      return;
    }

    const request = {
      sensorMacAddress,
      actuatorMacAddress: form.controls['actuatorMacAddress'].value,
      reading: form.controls['reading'].value,
      comparison: form.controls['comparison'].value,
      threshold: Number(form.controls['threshold'].value),
      actuatorState: form.controls['actuatorState'].value
    };

    this.policyApiService.CreatePolicy(request).subscribe({
      next: (policy) => {
        this.policies.update(policies => [...policies, policy]);
        this.policyForms.delete(sensorMacAddress);
        this.errorMessage.set(null);
      },
      error: (error: HttpErrorResponse) => this.showError(error, 'Could not create the automation.')
    });
  }

  protected deletePolicy(policy: PolicyResponse): void {
    if (!confirm(`Remove the automation driving ${policy.actuatorName}?`)) {
      return;
    }

    this.policyApiService.DeletePolicy(policy.id).subscribe({
      next: () => {
        this.policies.update(policies => policies.filter(x => x.id !== policy.id));
        this.errorMessage.set(null);
      },
      error: (error: HttpErrorResponse) => this.showError(error, 'Could not remove the automation.')
    });
  }

  /** The policies this sensor drives. */
  protected policiesBySensor(macAddress: string): PolicyResponse[] {
    return this.policies().filter(policy => policy.sensorMacAddress === macAddress);
  }

  protected roomNameFor(macAddress: string): string {
    return this.rooms().find(room => room.devices.some(device => device.macAddress === macAddress))?.name ?? '';
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
