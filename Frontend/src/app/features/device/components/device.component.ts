import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, input, model, output } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActuatorState } from '../../../shared/enumerations/actuator-state.enum';
import { Comparison } from '../../../shared/enumerations/comparison.enum';
import { DeviceType } from '../../../shared/enumerations/device-type.enum';
import { SensorReading } from '../../../shared/enumerations/sensor-reading.enum';
import { DeviceResponse } from '../../../shared/models/response/device-response.interface';
import { PolicyResponse } from '../../../shared/models/response/policy-response.interface';
import { RoomResponse } from '../../../shared/models/response/room-response.interface';
import { BaseTelemetry } from '../../../shared/models/telemetry/base-telemetry.interface';
import { PolicyApiService } from '../../../shared/services/policy-api.service';
import { TelemetryService } from '../../../shared/services/telemetry.service';
import { LedTelemetry } from '../models/telemetry/led-telemetry.interface';
import { LightTelemetry } from '../models/telemetry/light-telemetry.interface';
import { TempAndHumidTelemetry } from '../models/telemetry/temp-and-humid-telemetry.interface';
import { DeviceApiService } from '../services/device-api.service';

@Component({
  selector: 'li[app-device]',
  imports: [ReactiveFormsModule],
  templateUrl: './device.component.html'
})
export class DeviceComponent {
  private readonly deviceApiService = inject(DeviceApiService);
  private readonly policyApiService = inject(PolicyApiService);
  private readonly telemetryService = inject(TelemetryService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly ActuatorState = ActuatorState;
  protected readonly Comparison = Comparison;

  /** The device this row shows. */
  readonly device = input.required<DeviceResponse>();

  /** Every room the user owns, so a sensor can drive an actuator in another room. */
  readonly rooms = input.required<RoomResponse[]>();

  /** Every policy the user owns, shared with the other devices. */
  readonly policies = model.required<PolicyResponse[]>();

  /** Raised after the user confirms they want to unpair the device. */
  readonly unpairRequested = output<void>();

  /** Raised with an error to show, or null to clear it. */
  readonly errorMessageChange = output<string | null>();

  // Created on first use, since it needs the device input to pick the default reading.
  private automationForm: FormGroup | undefined;

  /** The policies this sensor drives. */
  protected readonly drivenPolicies = computed(() =>
    this.policies().filter(policy => policy.sensorMacAddress === this.device().macAddress));

  /** The policy driving this actuator, if one does. */
  protected policyFor(): PolicyResponse | undefined {
    return this.policies().find(policy => policy.actuatorMacAddress === this.device().macAddress);
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
        this.errorMessageChange.emit(null);
      },
      error: (error: HttpErrorResponse) => this.showError(error, 'Could not change the policy.')
    });
  }

  protected isSensor(): boolean {
    const deviceType = this.device().deviceType;
    return (deviceType === DeviceType.LightSensor) || (deviceType === DeviceType.TempAndHumidSensor);
  }

  /** The readings this sensor reports, so a light sensor cannot watch humidity. */
  protected readings(): SensorReading[] {
    const deviceType = this.device().deviceType;

    if (deviceType === DeviceType.LightSensor) {
      return [SensorReading.Light];
    }

    if (deviceType === DeviceType.TempAndHumidSensor) {
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

  protected policyForm(): FormGroup {
    if (this.automationForm === undefined) {
      this.automationForm = this.formBuilder.nonNullable.group({
        reading: [this.readings()[0] ?? SensorReading.Temperature, [Validators.required]],
        comparison: [Comparison.Above, [Validators.required]],
        threshold: [0, [Validators.required]],
        actuatorMacAddress: ['', [Validators.required]],
        actuatorState: [ActuatorState.On, [Validators.required]]
      });
    }

    return this.automationForm;
  }

  protected createPolicy(): void {
    const form = this.policyForm();

    if (form.invalid) {
      return;
    }

    const request = {
      sensorMacAddress: this.device().macAddress,
      actuatorMacAddress: form.controls['actuatorMacAddress'].value,
      reading: form.controls['reading'].value,
      comparison: form.controls['comparison'].value,
      threshold: Number(form.controls['threshold'].value),
      actuatorState: form.controls['actuatorState'].value
    };

    this.policyApiService.CreatePolicy(request).subscribe({
      next: (policy) => {
        this.policies.update(policies => [...policies, policy]);
        this.automationForm = undefined;
        this.errorMessageChange.emit(null);
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
        this.errorMessageChange.emit(null);
      },
      error: (error: HttpErrorResponse) => this.showError(error, 'Could not remove the automation.')
    });
  }

  protected roomNameFor(macAddress: string): string {
    return this.rooms().find(room => room.devices.some(device => device.macAddress === macAddress))?.name ?? '';
  }

  /** Groups a stored MAC into pairs for display: 98CDAC261D12 -> 98:CD:AC:26:1D:12 */
  protected formatMacAddress(macAddress: string): string {
    return macAddress.match(/.{1,2}/g)?.join(':') ?? macAddress;
  }

  protected telemetryFor(): BaseTelemetry | undefined {
    return this.telemetryService.telemetry()[TelemetryService.normalizeMacAddress(this.device().macAddress)];
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

  protected toggleActuator(telemetry: BaseTelemetry): void {
    const macAddress = this.device().macAddress;
    const actuatorState = (this.actuatorState(telemetry) === ActuatorState.On) ? ActuatorState.Off : ActuatorState.On;

    this.deviceApiService.SetActuatorState({ macAddress, actuatorState }).subscribe({
      error: (error: HttpErrorResponse) => this.showError(error, 'Could not switch the device.')
    });
  }

  protected unpair(): void {
    if (!confirm(`Unpair ${this.formatMacAddress(this.device().macAddress)}?`)) {
      return;
    }

    this.unpairRequested.emit();
  }

  private isActuatorDevice(device: DeviceResponse): boolean {
    return (device.deviceType === DeviceType.LedActuator) || (device.deviceType === DeviceType.FanActuator);
  }

  private showError(error: HttpErrorResponse, fallback: string): void {
    this.errorMessageChange.emit(typeof error.error === 'string' ? error.error : fallback);
  }
}
