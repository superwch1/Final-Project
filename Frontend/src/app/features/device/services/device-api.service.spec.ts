import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActuatorState } from '../../../shared/enumerations/actuator-state.enum';
import { DeviceApiService } from './device-api.service';

describe('DeviceApiService', () => {
  let service: DeviceApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(DeviceApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('posts the actuator state to device/actuator/state', () => {
    const request = { macAddress: 'AABBCCDDEE02', actuatorState: ActuatorState.On };

    service.SetActuatorState(request).subscribe();

    expect(http.expectOne({ method: 'POST', url: 'device/actuator/state' }).request.body).toEqual(request);
  });
});
