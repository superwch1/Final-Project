import { DeviceType } from "../../enumerations/device-type.enum";

export interface DeviceResponse {
  macAddress: string;
  name: string;
  deviceType: DeviceType;
}
