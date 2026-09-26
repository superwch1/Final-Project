import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActuatorState } from '../enumerations/actuator-state.enum';
import { Comparison } from '../enumerations/comparison.enum';
import { SensorReading } from '../enumerations/sensor-reading.enum';
import { PolicyApiService } from './policy-api.service';

describe('PolicyApiService', () => {
  let service: PolicyApiService;
  let http: HttpTestingController;

  const rule = {
    reading: SensorReading.Light,
    comparison: Comparison.Above,
    threshold: 50,
    actuatorState: ActuatorState.On
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(PolicyApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('gets policies from policy', () => {
    service.GetPolicies().subscribe();

    expect(http.expectOne({ method: 'GET', url: 'policy' })).toBeTruthy();
  });

  it('posts a new policy to policy', () => {
    const request = { sensorMacAddress: 'AABBCCDDEE01', actuatorMacAddress: 'AABBCCDDEE02', ...rule };

    service.CreatePolicy(request).subscribe();

    expect(http.expectOne({ method: 'POST', url: 'policy' }).request.body).toEqual(request);
  });

  it('puts an update to policy/{id}', () => {
    service.UpdatePolicy('policy-1', { ...rule, isEnabled: false }).subscribe();

    expect(http.expectOne({ method: 'PUT', url: 'policy/policy-1' })).toBeTruthy();
  });

  it('deletes policy/{id}', () => {
    service.DeletePolicy('policy-1').subscribe();

    expect(http.expectOne({ method: 'DELETE', url: 'policy/policy-1' })).toBeTruthy();
  });
});
