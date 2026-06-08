import { ActuatorState } from "../../enumerations/actuator-state.enum";
import { BaseTelemetry } from "./base-telemetry.interface";

export interface FanTelemetry extends BaseTelemetry {
  actuatorState: ActuatorState;
}