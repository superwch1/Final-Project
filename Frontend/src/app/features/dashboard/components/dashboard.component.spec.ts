import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { buttonWithText, clickButton, enterValue, submitForm, textOf } from '../../../../testing/dom';
import { expectShownAsPlainText, InjectionPayload, resetInjection } from '../../../../testing/injection';
import { room } from '../../../../testing/test-data';
import { AuthService } from '../../../shared/services/auth.service';
import { PolicyApiService } from '../../../shared/services/policy-api.service';
import { RoomApiService } from '../../../shared/services/room-api.service';
import { TelemetryService } from '../../../shared/services/telemetry.service';
import { DeviceApiService } from '../../device/services/device-api.service';
import { DashboardComponent } from './dashboard.component';

describe('DashboardComponent', () => {
  let fixture: ComponentFixture<DashboardComponent>;
  let authService: jasmine.SpyObj<AuthService>;
  let roomApiService: jasmine.SpyObj<RoomApiService>;
  let policyApiService: jasmine.SpyObj<PolicyApiService>;
  let telemetryService: jasmine.SpyObj<TelemetryService>;
  let isSignedIn: boolean;

  beforeEach(() => {
    isSignedIn = true;
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['isSignedIn', 'name', 'signOut']);
    authService.isSignedIn.and.callFake(() => isSignedIn);
    authService.name.and.returnValue('Alex');
    authService.signOut.and.callFake(() => isSignedIn = false);

    roomApiService = jasmine.createSpyObj<RoomApiService>('RoomApiService', ['GetRooms', 'CreateRoom', 'RenameRoom', 'DeleteRoom', 'PairDevice', 'UnpairDevice']);
    roomApiService.GetRooms.and.returnValue(of([room([], 'Living room')]));
    policyApiService = jasmine.createSpyObj<PolicyApiService>('PolicyApiService', ['GetPolicies', 'CreatePolicy', 'UpdatePolicy', 'DeletePolicy']);
    policyApiService.GetPolicies.and.returnValue(of([]));
    telemetryService = jasmine.createSpyObj<TelemetryService>('TelemetryService', ['connect', 'disconnect'], { telemetry: signal({}) });

    TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService },
        { provide: RoomApiService, useValue: roomApiService },
        { provide: PolicyApiService, useValue: policyApiService },
        { provide: TelemetryService, useValue: telemetryService },
        { provide: DeviceApiService, useValue: jasmine.createSpyObj('DeviceApiService', ['SetActuatorState']) }
      ]
    });
  });

  function render(): void {
    fixture = TestBed.createComponent(DashboardComponent);
    fixture.detectChanges();
  }

  it('offers sign in when signed out', () => {
    isSignedIn = false;

    render();

    expect(textOf(fixture)).toContain('Sign in');
  });

  it('does not load rooms when signed out', () => {
    isSignedIn = false;

    render();

    expect(roomApiService.GetRooms).not.toHaveBeenCalled();
  });

  it('does not open live telemetry when signed out', () => {
    isSignedIn = false;

    render();

    expect(telemetryService.connect).not.toHaveBeenCalled();
  });

  it('welcomes the signed-in user by name', () => {
    render();

    expect(textOf(fixture)).toContain('Welcome, Alex');
  });

  it('shows the rooms loaded for the signed-in user', () => {
    render();

    expect(textOf(fixture)).toContain('Living room');
  });

  it('loads the policies for the signed-in user', () => {
    render();

    expect(policyApiService.GetPolicies).toHaveBeenCalled();
  });

  it('opens live telemetry when signed in', () => {
    render();

    expect(telemetryService.connect).toHaveBeenCalled();
  });

  it('says when the user has no rooms', () => {
    roomApiService.GetRooms.and.returnValue(of([]));

    render();

    expect(textOf(fixture)).toContain('No rooms yet.');
  });

  it('shows an error when the rooms fail to load', () => {
    roomApiService.GetRooms.and.returnValue(throwError(() => new HttpErrorResponse({ status: 500 })));

    render();

    expect(textOf(fixture)).toContain('Could not load your rooms.');
  });

  it('disables create while the room name is empty', () => {
    render();

    expect(buttonWithText(fixture, 'Create')?.disabled).toBeTrue();
  });

  it('creates a room with the trimmed name', () => {
    roomApiService.CreateRoom.and.returnValue(of(room([], 'Kitchen', 'room-2')));
    render();

    enterValue(fixture, 'name', '  Kitchen ');
    submitForm(fixture);

    expect(roomApiService.CreateRoom).toHaveBeenCalledWith({ name: 'Kitchen' });
  });

  it('shows the new room after creating it', () => {
    roomApiService.CreateRoom.and.returnValue(of(room([], 'Kitchen', 'room-2')));
    render();

    enterValue(fixture, 'name', 'Kitchen');
    submitForm(fixture);

    expect(textOf(fixture)).toContain('Kitchen');
  });

  it('removes a room once its card deletes it', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    roomApiService.DeleteRoom.and.returnValue(of(undefined));
    render();

    clickButton(fixture, 'Delete');

    expect(textOf(fixture)).not.toContain('Living room');
  });

  it('shows a renamed room once its card renames it', () => {
    spyOn(window, 'prompt').and.returnValue('Kitchen');
    roomApiService.RenameRoom.and.returnValue(of(room([], 'Kitchen')));
    render();

    clickButton(fixture, 'Rename');

    expect(textOf(fixture)).toContain('Kitchen');
  });

  it('signs out when the user clicks sign out', () => {
    render();

    clickButton(fixture, 'Sign out');

    expect(authService.signOut).toHaveBeenCalled();
  });

  it('closes live telemetry when the user signs out', () => {
    render();

    clickButton(fixture, 'Sign out');

    expect(telemetryService.disconnect).toHaveBeenCalled();
  });

  it('hides the rooms after signing out', () => {
    render();

    clickButton(fixture, 'Sign out');

    expect(textOf(fixture)).not.toContain('Living room');
  });

  it('closes live telemetry when the page closes', () => {
    render();

    fixture.destroy();

    expect(telemetryService.disconnect).toHaveBeenCalled();
  });

  describe('script injection', () => {
    beforeEach(() => resetInjection());

    it('shows a room name containing HTML as plain text', async () => {
      roomApiService.GetRooms.and.returnValue(of([room([], InjectionPayload)]));

      render();

      await expectShownAsPlainText(fixture);
    });

    it('shows a user name containing HTML as plain text', async () => {
      authService.name.and.returnValue(InjectionPayload);

      render();

      await expectShownAsPlainText(fixture);
    });

    it('shows a server error containing HTML as plain text', async () => {
      roomApiService.GetRooms.and.returnValue(throwError(() => new HttpErrorResponse({ error: InjectionPayload, status: 500 })));

      render();

      await expectShownAsPlainText(fixture);
    });
  });
});
