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

    /** Creates an account */
    public CreateAccount(request: CreateAccountRequest) {
        return this.http.post<AuthResponse>("account", request);
    }

    /** Signs in with email and password */
    public Login(request: LoginRequest) {
        return this.http.post<AuthResponse>("account/login", request);
    }
}
