import { BaseTelemetry } from "./base-telemetry.interface";

export interface TempAndHumidTelemetry extends BaseTelemetry {
  temperatureReading: number;
  humidityReading: number;
}