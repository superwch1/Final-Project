import { ActuatorState } from "../../../../shared/enumerations/actuator-state.enum";
import { BaseDeviceRequest } from "./base-device-request.interface";

export interface SetActuatorStateRequest extends BaseDeviceRequest {
  actuatorState: ActuatorState
}