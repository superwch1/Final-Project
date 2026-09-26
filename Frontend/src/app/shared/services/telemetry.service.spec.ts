import { TestBed } from '@angular/core/testing';
import { FakeWebSocket } from '../../../testing/fake-web-socket';
import { DeviceType } from '../enumerations/device-type.enum';
import { ApiHost } from '../interceptors/api.interceptor';
import { AuthService } from './auth.service';
import { TelemetryService } from './telemetry.service';

describe('TelemetryService', () => {
  const realWebSocket = window.WebSocket;
  let service: TelemetryService;

  beforeEach(() => {
    FakeWebSocket.instances = [];
    (window as unknown as { WebSocket: unknown }).WebSocket = FakeWebSocket;

    const authService = jasmine.createSpyObj<AuthService>('AuthService', ['getToken']);
    authService.getToken.and.returnValue('token');

    TestBed.configureTestingModule({
      providers: [{ provide: AuthService, useValue: authService }]
    });

    service = TestBed.inject(TelemetryService);
  });

  afterEach(() => {
    service.disconnect();
    window.WebSocket = realWebSocket;
  });

  it('opens the dashboard socket on the backend host', () => {
    service.connect();

    expect(FakeWebSocket.latest().url).toBe(`ws://${ApiHost}/dashboard/ws`);
  });

  it('sends the access token once the socket opens', () => {
    service.connect();

    FakeWebSocket.latest().serverOpens();

    expect(FakeWebSocket.latest().sent).toEqual(['token']);
  });

  it('stores telemetry under the normalised MAC address', () => {
    service.connect();

    FakeWebSocket.latest().serverSends({ macAddress: 'aa:bb:cc:dd:ee:01', deviceType: DeviceType.LightSensor, lightReading: 42 });

    expect(service.telemetry()['AABBCCDDEE01']).toBeDefined();
  });

  it('keeps only the latest telemetry per device', () => {
    service.connect();

    FakeWebSocket.latest().serverSends({ macAddress: 'AABBCCDDEE01', deviceType: DeviceType.LightSensor, lightReading: 42 });
    FakeWebSocket.latest().serverSends({ macAddress: 'AABBCCDDEE01', deviceType: DeviceType.LightSensor, lightReading: 43 });

    expect((service.telemetry()['AABBCCDDEE01'] as unknown as { lightReading: number }).lightReading).toBe(43);
  });

  it('closes the socket on disconnect', () => {
    service.connect();
    const socket = FakeWebSocket.latest();

    service.disconnect();

    expect(socket.isClosed).toBeTrue();
  });

  it('reconnects after the server drops the connection', () => {
    jasmine.clock().install();
    service.connect();

    FakeWebSocket.latest().serverCloses();
    jasmine.clock().tick(5000);
    jasmine.clock().uninstall();

    expect(FakeWebSocket.instances.length).toBe(2);
  });

  it('does not reconnect after disconnect', () => {
    jasmine.clock().install();
    service.connect();
    const socket = FakeWebSocket.latest();

    service.disconnect();
    socket.serverCloses();
    jasmine.clock().tick(5000);
    jasmine.clock().uninstall();

    expect(FakeWebSocket.instances.length).toBe(1);
  });

  it('normalises a MAC address by removing separators and upper-casing', () => {
    expect(TelemetryService.normalizeMacAddress('aa-bb:cc-dd:ee-01')).toBe('AABBCCDDEE01');
  });
});
