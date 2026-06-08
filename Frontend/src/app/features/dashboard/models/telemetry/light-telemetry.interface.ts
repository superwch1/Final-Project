import { BaseTelemetry } from "./base-telemetry.interface";

export interface LightTelemetry extends BaseTelemetry {
  lightReading: number;
}