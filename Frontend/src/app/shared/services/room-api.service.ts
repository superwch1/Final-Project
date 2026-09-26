import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { CreateRoomRequest } from "../models/request/create-room-request.interface";
import { PairDeviceRequest } from "../models/request/pair-device-request.interface";
import { UpdateRoomRequest } from "../models/request/update-room-request.interface";
import { DeviceResponse } from "../models/response/device-response.interface";
import { RoomResponse } from "../models/response/room-response.interface";
import { AuthService } from "./auth.service";

@Injectable({
    providedIn: 'root'
})

export class RoomApiService {
    private readonly http = inject(HttpClient);
    private readonly authService = inject(AuthService);
    private readonly baseUrl = "http://192.168.1.7:5000/room";

    public GetRooms() {
        return this.http.get<RoomResponse[]>(this.baseUrl, { headers: this.headers() });
    }

    public CreateRoom(request: CreateRoomRequest) {
        return this.http.post<RoomResponse>(this.baseUrl, request, { headers: this.headers() });
    }

    public RenameRoom(roomId: string, request: UpdateRoomRequest) {
        return this.http.put<RoomResponse>(`${this.baseUrl}/${roomId}`, request, { headers: this.headers() });
    }

    public DeleteRoom(roomId: string) {
        return this.http.delete<void>(`${this.baseUrl}/${roomId}`, { headers: this.headers() });
    }

    public PairDevice(roomId: string, request: PairDeviceRequest) {
        return this.http.post<DeviceResponse>(`${this.baseUrl}/${roomId}/device`, request, { headers: this.headers() });
    }

    public UnpairDevice(roomId: string, macAddress: string) {
        return this.http.delete<void>(`${this.baseUrl}/${roomId}/device/${macAddress}`, { headers: this.headers() });
    }

    private headers() {
        return { Authorization: `Bearer ${this.authService.getToken() ?? ''}` };
    }
}
