import { ActuatorState } from '../app/shared/enumerations/actuator-state.enum';
import { Comparison } from '../app/shared/enumerations/comparison.enum';
import { DeviceType } from '../app/shared/enumerations/device-type.enum';
import { SensorReading } from '../app/shared/enumerations/sensor-reading.enum';
import { AccessTokenPayload } from '../app/shared/models/access-token-payload.interface';
import { DeviceResponse } from '../app/shared/models/response/device-response.interface';
import { PolicyResponse } from '../app/shared/models/response/policy-response.interface';
import { RoomResponse } from '../app/shared/models/response/room-response.interface';

export const SensorMacAddress = 'AABBCCDDEE01';
export const ActuatorMacAddress = 'AABBCCDDEE02';

/** Build an unsigned JWT carrying */
export function fakeToken(payload: AccessTokenPayload): string {
  const encode = (value: object) => btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${encode({ alg: 'none' })}.${encode(payload)}.signature`;
}

/** Return a Unix time in seconds */
export function secondsFromNow(seconds: number): number {
  return Math.floor(Date.now() / 1000) + seconds;
}

/** Build a device response */
export function device(macAddress: string, deviceType: DeviceType, name = 'Device'): DeviceResponse {
  return { macAddress, deviceType, name };
}

/** Build a room response */
export function room(devices: DeviceResponse[] = [], name = 'Living room', id = 'room-1'): RoomResponse {
  return { id, name, devices };
}

/** Build an policy that */
export function policy(overrides: Partial<PolicyResponse> = {}): PolicyResponse {
  return {
    id: 'policy-1',
    sensorMacAddress: SensorMacAddress,
    sensorName: 'Light sensor',
    actuatorMacAddress: ActuatorMacAddress,
    actuatorName: 'Desk lamp',
    reading: SensorReading.Light,
    comparison: Comparison.Above,
    threshold: 50,
    actuatorState: ActuatorState.On,
    isEnabled: true,
    ...overrides
  };
}
