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

  readonly device = input.required<DeviceResponse>();
  readonly rooms = input.required<RoomResponse[]>();
  readonly policies = model.required<PolicyResponse[]>();
  readonly unpairRequested = output<void>();
  readonly errorMessageChange = output<string | null>();

  private automationForm: FormGroup | undefined;

  protected readonly drivenPolicies = computed(() =>
    this.policies().filter(policy => policy.sensorMacAddress === this.device().macAddress));

  /** Return the policy driving this actuator */
  protected policyFor(): PolicyResponse | undefined {
    return this.policies().find(policy => policy.actuatorMacAddress === this.device().macAddress);
  }

  /** Override the existing policy, or return the control */
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

  /** Return true if this device is a sensor */
  protected isSensor(): boolean {
    const deviceType = this.device().deviceType;
    return (deviceType === DeviceType.LightSensor) || (deviceType === DeviceType.TempAndHumidSensor);
  }

  /** Return the readings this sensor */
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

  /** Return actuators without policy yet */
  protected availableActuators(): { device: DeviceResponse; roomName: string }[] {
    const driven = new Set(this.policies().map(policy => policy.actuatorMacAddress));

    return this.rooms()
      .flatMap(room => room.devices.map(device => ({ device, roomName: room.name })))
      .filter(entry => this.isActuatorDevice(entry.device) && !driven.has(entry.device.macAddress));
  }

  /** Return the automation form */
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

  /** Create a policy */
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

  /** Deletes a policy */
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

  /** Return the name of the room */
  protected roomNameFor(macAddress: string): string {
    return this.rooms().find(room => room.devices.some(device => device.macAddress === macAddress))?.name ?? '';
  }

  /** Format a stored MAC address */
  protected formatMacAddress(macAddress: string): string {
    return macAddress.match(/.{1,2}/g)?.join(':') ?? macAddress;
  }

  /** Return the latest telemetry */
  protected telemetryFor(): BaseTelemetry | undefined {
    return this.telemetryService.telemetry()[TelemetryService.normalizeMacAddress(this.device().macAddress)];
  }

  /** Return true if the telemetry comes from an actuator */
  protected isActuator(telemetry: BaseTelemetry): boolean {
    return (telemetry.deviceType === DeviceType.LedActuator) || (telemetry.deviceType === DeviceType.FanActuator);
  }

  /** Return the current actuator state */
  protected actuatorState(telemetry: BaseTelemetry): ActuatorState {
    return (telemetry as LedTelemetry).actuatorState;
  }

  /** Format a sensor reading */
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

  /** Switch the actuator to the opposite state */
  protected toggleActuator(telemetry: BaseTelemetry): void {
    const macAddress = this.device().macAddress;
    const actuatorState = (this.actuatorState(telemetry) === ActuatorState.On) ? ActuatorState.Off : ActuatorState.On;

    this.deviceApiService.SetActuatorState({ macAddress, actuatorState }).subscribe({
      error: (error: HttpErrorResponse) => this.showError(error, 'Could not switch the device.')
    });
  }

  /** Ask the user to confirm before unpair this device */
  protected unpair(): void {
    if (!confirm(`Unpair ${this.formatMacAddress(this.device().macAddress)}?`)) {
      return;
    }

    this.unpairRequested.emit();
  }

  /** Return true if the device is an actuator */
  private isActuatorDevice(device: DeviceResponse): boolean {
    return (device.deviceType === DeviceType.LedActuator) || (device.deviceType === DeviceType.FanActuator);
  }

  /** Shows the error message */
  private showError(error: HttpErrorResponse, fallback: string): void {
    this.errorMessageChange.emit(typeof error.error === 'string' ? error.error : fallback);
  }
}
