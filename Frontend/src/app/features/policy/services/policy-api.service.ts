import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { AuthService } from "../../account/services/auth.service";
import { CreatePolicyRequest } from "../models/create-policy-request.interface";
import { PolicyResponse } from "../models/policy-response.interface";
import { UpdatePolicyRequest } from "../models/update-policy-request.interface";

@Injectable({
    providedIn: 'root'
})

export class PolicyApiService {
    private readonly http = inject(HttpClient);
    private readonly authService = inject(AuthService);
    private readonly baseUrl = "http://192.168.1.7:5000/policy";

    public GetPolicies() {
        return this.http.get<PolicyResponse[]>(this.baseUrl, { headers: this.headers() });
    }

    public CreatePolicy(request: CreatePolicyRequest) {
        return this.http.post<PolicyResponse>(this.baseUrl, request, { headers: this.headers() });
    }

    public UpdatePolicy(policyId: string, request: UpdatePolicyRequest) {
        return this.http.put<PolicyResponse>(`${this.baseUrl}/${policyId}`, request, { headers: this.headers() });
    }

    public DeletePolicy(policyId: string) {
        return this.http.delete<void>(`${this.baseUrl}/${policyId}`, { headers: this.headers() });
    }

    private headers() {
        return { Authorization: `Bearer ${this.authService.getToken() ?? ''}` };
    }
}
