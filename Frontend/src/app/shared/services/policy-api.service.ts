import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { CreatePolicyRequest } from "../models/request/create-policy-request.interface";
import { UpdatePolicyRequest } from "../models/request/update-policy-request.interface";
import { PolicyResponse } from "../models/response/policy-response.interface";
import { AuthService } from "./auth.service";

@Injectable({
    providedIn: 'root'
})

export class PolicyApiService {
    private readonly http = inject(HttpClient);
    private readonly authService = inject(AuthService);
    private readonly baseUrl = "http://192.168.1.7:5000/policy";

    /** Return every policy owned by the account */
    public GetPolicies() {
        return this.http.get<PolicyResponse[]>(this.baseUrl, { headers: this.headers() });
    }

    /** Create a policy */
    public CreatePolicy(request: CreatePolicyRequest) {
        return this.http.post<PolicyResponse>(this.baseUrl, request, { headers: this.headers() });
    }

    /** update a policy */
    public UpdatePolicy(policyId: string, request: UpdatePolicyRequest) {
        return this.http.put<PolicyResponse>(`${this.baseUrl}/${policyId}`, request, { headers: this.headers() });
    }

    /** Delete a policy */
    public DeletePolicy(policyId: string) {
        return this.http.delete<void>(`${this.baseUrl}/${policyId}`, { headers: this.headers() });
    }

    /** Return the authorization header with the access token */
    private headers() {
        return { Authorization: `Bearer ${this.authService.getToken() ?? ''}` };
    }
}
