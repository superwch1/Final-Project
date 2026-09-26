import { ActuatorState } from "../../../../shared/enumerations/actuator-state.enum";
import { BaseTelemetry } from "../../../../shared/models/telemetry/base-telemetry.interface";

export interface LedTelemetry extends BaseTelemetry {
  actuatorState: ActuatorState;
}