import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { CreateAccountRequest } from "../models/request/create-account-request.interface";
import { LoginRequest } from "../models/request/login-request.interface";
import { AuthResponse } from "../models/response/auth-response.interface";

@Injectable({
    providedIn: 'root'
})

export class AccountApiService {
    private readonly http = inject(HttpClient);

    public CreateAccount(request: CreateAccountRequest) {
        return this.http.post<AuthResponse>("http://192.168.1.7:5000/account", request);
    }

    public Login(request: LoginRequest) {
        return this.http.post<AuthResponse>("http://192.168.1.7:5000/account/login", request);
    }
}
