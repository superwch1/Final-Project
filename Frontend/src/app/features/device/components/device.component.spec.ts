import { HttpErrorResponse } from '@angular/common/http';
import { signal, WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { buttonWithText, clickButton, enterValue, submitForm, textOf } from '../../../../testing/dom';
import { expectShownAsPlainText, InjectionPayload, resetInjection } from '../../../../testing/injection';
import { ActuatorMacAddress, device, policy, room, SensorMacAddress } from '../../../../testing/test-data';
import { ActuatorState } from '../../../shared/enumerations/actuator-state.enum';
import { DeviceType } from '../../../shared/enumerations/device-type.enum';
import { DeviceResponse } from '../../../shared/models/response/device-response.interface';
import { PolicyResponse } from '../../../shared/models/response/policy-response.interface';
import { BaseTelemetry } from '../../../shared/models/telemetry/base-telemetry.interface';
import { PolicyApiService } from '../../../shared/services/policy-api.service';
import { TelemetryService } from '../../../shared/services/telemetry.service';
import { DeviceApiService } from '../services/device-api.service';
import { DeviceComponent } from './device.component';

describe('DeviceComponent', () => {
  const sensor = device(SensorMacAddress, DeviceType.LightSensor, 'Light sensor');
  const actuator = device(ActuatorMacAddress, DeviceType.LedActuator, 'Desk lamp');

  let fixture: ComponentFixture<DeviceComponent>;
  let deviceApiService: jasmine.SpyObj<DeviceApiService>;
  let policyApiService: jasmine.SpyObj<PolicyApiService>;
  let telemetry: WritableSignal<Record<string, BaseTelemetry>>;

  beforeEach(() => {
    deviceApiService = jasmine.createSpyObj<DeviceApiService>('DeviceApiService', ['SetActuatorState']);
    deviceApiService.SetActuatorState.and.returnValue(of(undefined));
    policyApiService = jasmine.createSpyObj<PolicyApiService>('PolicyApiService', ['CreatePolicy', 'UpdatePolicy', 'DeletePolicy']);
    telemetry = signal({});

    TestBed.configureTestingModule({
      imports: [DeviceComponent],
      providers: [
        { provide: DeviceApiService, useValue: deviceApiService },
        { provide: PolicyApiService, useValue: policyApiService },
        { provide: TelemetryService, useValue: { telemetry } }
      ]
    });

    fixture = TestBed.createComponent(DeviceComponent);
  });

  function render(shown: DeviceResponse, policies: PolicyResponse[] = [], devices = [sensor, actuator]): void {
    fixture.componentRef.setInput('device', shown);
    fixture.componentRef.setInput('rooms', [room(devices)]);
    fixture.componentRef.setInput('policies', policies);
    fixture.detectChanges();
  }

  function receive(reading: object): void {
    telemetry.set({ [(reading as BaseTelemetry).macAddress]: reading as BaseTelemetry });
    fixture.detectChanges();
  }

  it('shows the MAC address in pairs', () => {
    render(sensor);

    expect(textOf(fixture)).toContain('AA:BB:CC:DD:EE:01');
  });

  it('waits for the device before any telemetry arrives', () => {
    render(sensor);

    expect(textOf(fixture)).toContain('Waiting for the device');
  });

  it('shows a light reading as a percentage', () => {
    render(sensor);

    receive({ macAddress: SensorMacAddress, deviceType: DeviceType.LightSensor, lightReading: 42 });

    expect(textOf(fixture)).toContain('42 % light');
  });

  it('shows temperature and humidity together', () => {
    render(device(SensorMacAddress, DeviceType.TempAndHumidSensor));

    receive({ macAddress: SensorMacAddress, deviceType: DeviceType.TempAndHumidSensor, temperatureReading: 21.5, humidityReading: 60 });

    expect(textOf(fixture)).toContain('21.5 °C, 60 % humidity');
  });

  it('offers to switch on an actuator that is off', () => {
    render(actuator);

    receive({ macAddress: ActuatorMacAddress, deviceType: DeviceType.LedActuator, actuatorState: ActuatorState.Off });

    expect(buttonWithText(fixture, 'Switch on')).toBeDefined();
  });

  it('switches the actuator to the opposite state', () => {
    render(actuator);
    receive({ macAddress: ActuatorMacAddress, deviceType: DeviceType.LedActuator, actuatorState: ActuatorState.Off });

    clickButton(fixture, 'Switch on');

    expect(deviceApiService.SetActuatorState).toHaveBeenCalledWith({ macAddress: ActuatorMacAddress, actuatorState: ActuatorState.On });
  });

  it('hides the switch while an enabled policy drives the actuator', () => {
    render(actuator, [policy()]);

    receive({ macAddress: ActuatorMacAddress, deviceType: DeviceType.LedActuator, actuatorState: ActuatorState.Off });

    expect(buttonWithText(fixture, 'Switch on')).toBeUndefined();
  });

  it('describes the policy driving the actuator', () => {
    render(actuator, [policy()]);

    expect(textOf(fixture)).toContain('Driven by Light sensor');
  });

  it('disables the policy when taking control', () => {
    policyApiService.UpdatePolicy.and.returnValue(of(policy({ isEnabled: false })));
    render(actuator, [policy()]);

    clickButton(fixture, 'Take control');

    expect(policyApiService.UpdatePolicy).toHaveBeenCalledWith('policy-1', jasmine.objectContaining({ isEnabled: false }));
  });

  it('shares the updated policy after taking control', () => {
    policyApiService.UpdatePolicy.and.returnValue(of(policy({ isEnabled: false })));
    render(actuator, [policy()]);

    clickButton(fixture, 'Take control');

    expect(fixture.componentInstance.policies()[0].isEnabled).toBeFalse();
  });

  it('removes the policy after the user confirms', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    policyApiService.DeletePolicy.and.returnValue(of(undefined));
    render(actuator, [policy()]);

    clickButton(fixture, 'Remove');

    expect(fixture.componentInstance.policies()).toEqual([]);
  });

  it('keeps the policy when the user cancels removing it', () => {
    spyOn(window, 'confirm').and.returnValue(false);
    render(actuator, [policy()]);

    clickButton(fixture, 'Remove');

    expect(policyApiService.DeletePolicy).not.toHaveBeenCalled();
  });

  it('lists the actuators this sensor drives', () => {
    render(sensor, [policy()]);

    expect(textOf(fixture)).toContain('Drives Desk lamp');
  });

  it('explains when every actuator is already driven', () => {
    render(sensor, [policy()]);

    expect(textOf(fixture)).toContain('Every actuator is already driven');
  });

  it('creates a policy from the automation form', () => {
    policyApiService.CreatePolicy.and.returnValue(of(policy()));
    render(sensor);

    enterValue(fixture, 'actuatorMacAddress', ActuatorMacAddress);
    submitForm(fixture);

    expect(policyApiService.CreatePolicy).toHaveBeenCalledWith(jasmine.objectContaining({ sensorMacAddress: SensorMacAddress, actuatorMacAddress: ActuatorMacAddress }));
  });

  it('shares the new policy after creating it', () => {
    policyApiService.CreatePolicy.and.returnValue(of(policy()));
    render(sensor);

    enterValue(fixture, 'actuatorMacAddress', ActuatorMacAddress);
    submitForm(fixture);

    expect(fixture.componentInstance.policies().length).toBe(1);
  });

  it('asks the room to unpair after the user confirms', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    render(sensor);
    let isRequested = false;
    fixture.componentInstance.unpairRequested.subscribe(() => isRequested = true);

    clickButton(fixture, 'Unpair');

    expect(isRequested).toBeTrue();
  });

  it('does not unpair when the user cancels', () => {
    spyOn(window, 'confirm').and.returnValue(false);
    render(sensor);
    let isRequested = false;
    fixture.componentInstance.unpairRequested.subscribe(() => isRequested = true);

    clickButton(fixture, 'Unpair');

    expect(isRequested).toBeFalse();
  });

  it('reports a fallback error when switching fails', () => {
    deviceApiService.SetActuatorState.and.returnValue(throwError(() => new HttpErrorResponse({ status: 500 })));
    render(actuator);
    receive({ macAddress: ActuatorMacAddress, deviceType: DeviceType.LedActuator, actuatorState: ActuatorState.Off });
    const errorMessages: (string | null)[] = [];
    fixture.componentInstance.errorMessageChange.subscribe(message => errorMessages.push(message));

    clickButton(fixture, 'Switch on');

    expect(errorMessages).toEqual(['Could not switch the device.']);
  });

  describe('script injection', () => {
    beforeEach(() => resetInjection());

    it('shows a device name containing HTML as plain text', async () => {
      render(device(SensorMacAddress, DeviceType.LightSensor, InjectionPayload));

      await expectShownAsPlainText(fixture);
    });

    it('shows a policy sensor name containing HTML as plain text', async () => {
      render(actuator, [policy({ sensorName: InjectionPayload })]);

      await expectShownAsPlainText(fixture);
    });

    it('shows a driven actuator name containing HTML as plain text', async () => {
      render(sensor, [policy({ actuatorName: InjectionPayload })]);

      await expectShownAsPlainText(fixture);
    });

    it('shows a room name containing HTML as plain text in the actuator list', async () => {
      render(sensor, [], [sensor, actuator]);
      fixture.componentRef.setInput('rooms', [room([sensor, actuator], InjectionPayload)]);
      fixture.detectChanges();

      await expectShownAsPlainText(fixture);
    });

    it('shows live telemetry containing HTML as plain text', async () => {
      render(sensor);

      receive({ macAddress: SensorMacAddress, deviceType: DeviceType.LightSensor, lightReading: InjectionPayload });

      await expectShownAsPlainText(fixture);
    });
  });
});
