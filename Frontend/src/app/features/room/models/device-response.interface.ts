import { DeviceType } from "../../dashboard/enumerations/device-type.enum";

export interface DeviceResponse {
  macAddress: string;
  name: string;

  /** Unknown until the board has reported at least once. */
  deviceType: DeviceType;
}
