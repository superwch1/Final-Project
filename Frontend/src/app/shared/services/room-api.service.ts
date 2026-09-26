import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { CreateRoomRequest } from "../models/request/create-room-request.interface";
import { PairDeviceRequest } from "../models/request/pair-device-request.interface";
import { UpdateRoomRequest } from "../models/request/update-room-request.interface";
import { DeviceResponse } from "../models/response/device-response.interface";
import { RoomResponse } from "../models/response/room-response.interface";

@Injectable({
    providedIn: 'root'
})

export class RoomApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = "room";

    /** Return every room owned by the account */
    public GetRooms() {
        return this.http.get<RoomResponse[]>(this.baseUrl);
    }

    /** Create a room */
    public CreateRoom(request: CreateRoomRequest) {
        return this.http.post<RoomResponse>(this.baseUrl, request);
    }

    /** Rename a room */
    public RenameRoom(roomId: string, request: UpdateRoomRequest) {
        return this.http.put<RoomResponse>(`${this.baseUrl}/${roomId}`, request);
    }

    /** Delete a room and unpair its devices */
    public DeleteRoom(roomId: string) {
        return this.http.delete<void>(`${this.baseUrl}/${roomId}`);
    }

    /** Pair a device to a room */
    public PairDevice(roomId: string, request: PairDeviceRequest) {
        return this.http.post<DeviceResponse>(`${this.baseUrl}/${roomId}/device`, request);
    }

    /** Remove a device from a room */
    public UnpairDevice(roomId: string, macAddress: string) {
        return this.http.delete<void>(`${this.baseUrl}/${roomId}/device/${macAddress}`);
    }
}
