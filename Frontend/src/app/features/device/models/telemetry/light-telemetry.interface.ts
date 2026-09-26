import { BaseTelemetry } from "../../../../shared/models/telemetry/base-telemetry.interface";

export interface LightTelemetry extends BaseTelemetry {
  lightReading: number;
}