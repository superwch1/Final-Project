import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { buttonWithText, clickButton, enterValue, submitForm, textOf } from '../../../../testing/dom';
import { expectShownAsPlainText, InjectionPayload, resetInjection } from '../../../../testing/injection';
import { device, room, SensorMacAddress } from '../../../../testing/test-data';
import { DeviceType } from '../../../shared/enumerations/device-type.enum';
import { RoomResponse } from '../../../shared/models/response/room-response.interface';
import { PolicyApiService } from '../../../shared/services/policy-api.service';
import { RoomApiService } from '../../../shared/services/room-api.service';
import { TelemetryService } from '../../../shared/services/telemetry.service';
import { DeviceApiService } from '../../device/services/device-api.service';
import { RoomComponent } from './room.component';

describe('RoomComponent', () => {
  const sensor = device(SensorMacAddress, DeviceType.LightSensor, 'Light sensor');

  let fixture: ComponentFixture<RoomComponent>;
  let roomApiService: jasmine.SpyObj<RoomApiService>;

  beforeEach(() => {
    roomApiService = jasmine.createSpyObj<RoomApiService>('RoomApiService', ['RenameRoom', 'DeleteRoom', 'PairDevice', 'UnpairDevice']);

    TestBed.configureTestingModule({
      imports: [RoomComponent],
      providers: [
        { provide: RoomApiService, useValue: roomApiService },
        { provide: DeviceApiService, useValue: jasmine.createSpyObj('DeviceApiService', ['SetActuatorState']) },
        { provide: PolicyApiService, useValue: jasmine.createSpyObj('PolicyApiService', ['CreatePolicy', 'UpdatePolicy', 'DeletePolicy']) },
        { provide: TelemetryService, useValue: { telemetry: signal({}) } }
      ]
    });

    fixture = TestBed.createComponent(RoomComponent);
  });

  function render(shown: RoomResponse): void {
    fixture.componentRef.setInput('room', shown);
    fixture.componentRef.setInput('rooms', [shown]);
    fixture.componentRef.setInput('policies', []);
    fixture.detectChanges();
  }

  function updatedRooms(): RoomResponse[] {
    const updated: RoomResponse[] = [];
    fixture.componentInstance.roomUpdated.subscribe(value => updated.push(value));
    return updated;
  }

  it('shows the room name', () => {
    render(room([], 'Kitchen'));

    expect(textOf(fixture)).toContain('Kitchen');
  });

  it('says when no devices are paired', () => {
    render(room());

    expect(textOf(fixture)).toContain('No devices paired.');
  });

  it('shows a row for each paired device', () => {
    render(room([sensor]));

    expect(textOf(fixture)).toContain('Light sensor');
  });

  it('renames the room to the name the user enters', () => {
    spyOn(window, 'prompt').and.returnValue('  Kitchen ');
    roomApiService.RenameRoom.and.returnValue(of(room([], 'Kitchen')));
    render(room());

    clickButton(fixture, 'Rename');

    expect(roomApiService.RenameRoom).toHaveBeenCalledWith('room-1', { name: 'Kitchen' });
  });

  it('reports the renamed room', () => {
    spyOn(window, 'prompt').and.returnValue('Kitchen');
    roomApiService.RenameRoom.and.returnValue(of(room([], 'Kitchen')));
    render(room());
    const updated = updatedRooms();

    clickButton(fixture, 'Rename');

    expect(updated[0].name).toBe('Kitchen');
  });

  it('does not rename when the user cancels the prompt', () => {
    spyOn(window, 'prompt').and.returnValue(null);
    render(room());

    clickButton(fixture, 'Rename');

    expect(roomApiService.RenameRoom).not.toHaveBeenCalled();
  });

  it('reports the deleted room after the user confirms', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    roomApiService.DeleteRoom.and.returnValue(of(undefined));
    render(room());
    let deletedId: string | undefined;
    fixture.componentInstance.roomDeleted.subscribe(id => deletedId = id);

    clickButton(fixture, 'Delete');

    expect(deletedId).toBe('room-1');
  });

  it('does not delete when the user cancels', () => {
    spyOn(window, 'confirm').and.returnValue(false);
    render(room());

    clickButton(fixture, 'Delete');

    expect(roomApiService.DeleteRoom).not.toHaveBeenCalled();
  });

  it('disables pairing for an invalid MAC address', () => {
    render(room());

    enterValue(fixture, 'macAddress', 'AA:BB');
    enterValue(fixture, 'name', 'Lamp');

    expect(buttonWithText(fixture, 'Pair')?.disabled).toBeTrue();
  });

  it('pairs a device with the trimmed MAC address and name', () => {
    roomApiService.PairDevice.and.returnValue(of(sensor));
    render(room());

    enterValue(fixture, 'macAddress', ' AA:BB:CC:DD:EE:01 ');
    enterValue(fixture, 'name', ' Light sensor ');
    submitForm(fixture);

    expect(roomApiService.PairDevice).toHaveBeenCalledWith('room-1', { macAddress: 'AA:BB:CC:DD:EE:01', name: 'Light sensor' });
  });

  it('reports the room with the newly paired device', () => {
    roomApiService.PairDevice.and.returnValue(of(sensor));
    render(room());
    const updated = updatedRooms();

    enterValue(fixture, 'macAddress', 'AA:BB:CC:DD:EE:01');
    enterValue(fixture, 'name', 'Light sensor');
    submitForm(fixture);

    expect(updated[0].devices).toEqual([sensor]);
  });

  it('reports the server error when pairing fails', () => {
    roomApiService.PairDevice.and.returnValue(throwError(() => new HttpErrorResponse({ error: 'That device is already paired.', status: 409 })));
    render(room());
    const errorMessages: (string | null)[] = [];
    fixture.componentInstance.errorMessageChange.subscribe(message => errorMessages.push(message));

    enterValue(fixture, 'macAddress', 'AA:BB:CC:DD:EE:01');
    enterValue(fixture, 'name', 'Light sensor');
    submitForm(fixture);

    expect(errorMessages).toEqual(['That device is already paired.']);
  });

  it('unpairs the device when its row asks to', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    roomApiService.UnpairDevice.and.returnValue(of(undefined));
    render(room([sensor]));

    clickButton(fixture, 'Unpair');

    expect(roomApiService.UnpairDevice).toHaveBeenCalledWith('room-1', SensorMacAddress);
  });

  it('reports the room without the unpaired device', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    roomApiService.UnpairDevice.and.returnValue(of(undefined));
    render(room([sensor]));
    const updated = updatedRooms();

    clickButton(fixture, 'Unpair');

    expect(updated[0].devices).toEqual([]);
  });

  describe('script injection', () => {
    beforeEach(() => resetInjection());

    it('shows a room name containing HTML as plain text', async () => {
      render(room([], InjectionPayload));

      await expectShownAsPlainText(fixture);
    });

    it('shows a device name containing HTML as plain text', async () => {
      render(room([device(SensorMacAddress, DeviceType.LightSensor, InjectionPayload)]));

      await expectShownAsPlainText(fixture);
    });
  });
});
