import { BaseTelemetry } from "../../../../shared/models/telemetry/base-telemetry.interface";

export interface TempAndHumidTelemetry extends BaseTelemetry {
  temperatureReading: number;
  humidityReading: number;
}