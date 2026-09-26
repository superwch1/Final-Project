import { DeviceType } from "../../enumerations/device-type.enum";

export interface BaseTelemetry {
  wiFiSignal: number;
  freeHeap: number;
  macAddress: string;
  deviceType: DeviceType;
}