import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { AuthService } from "../../../shared/services/auth.service";
import { SetActuatorStateRequest } from "../models/request/set-actuator-state-request.interface";

@Injectable({
    providedIn: 'root'
})

export class DeviceApiService {
    private readonly http = inject(HttpClient);
    private readonly authService = inject(AuthService);

    public SetActuatorState(request: SetActuatorStateRequest) {
        return this.http.post<void>("http://192.168.1.7:5000/device/actuator/state", request, {
            headers: { Authorization: `Bearer ${this.authService.getToken() ?? ''}` }
        });
    }
}
