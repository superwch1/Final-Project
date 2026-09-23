import { DeviceResponse } from "./device-response.interface";

export interface RoomResponse {
  id: string;
  name: string;
  devices: DeviceResponse[];
}
