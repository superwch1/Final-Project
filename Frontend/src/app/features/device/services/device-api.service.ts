import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { SetActuatorStateRequest } from "../models/request/set-actuator-state-request.interface";

@Injectable({
    providedIn: 'root'
})

export class DeviceApiService {
    private readonly http = inject(HttpClient);

    /** Switches an actuator on or off */
    public SetActuatorState(request: SetActuatorStateRequest) {
        return this.http.post<void>("device/actuator/state", request);
    }
}
