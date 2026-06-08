import { ActuatorState } from "../../enumerations/actuator-state.enum";
import { BaseTelemetry } from "./base-telemetry.interface";

export interface LedTelemetry extends BaseTelemetry {
  actuatorState: ActuatorState;
}