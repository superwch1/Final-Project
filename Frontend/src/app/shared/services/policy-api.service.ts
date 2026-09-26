import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { CreatePolicyRequest } from "../models/request/create-policy-request.interface";
import { UpdatePolicyRequest } from "../models/request/update-policy-request.interface";
import { PolicyResponse } from "../models/response/policy-response.interface";

@Injectable({
    providedIn: 'root'
})

export class PolicyApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = "policy";

    /** Return every policy owned by the account */
    public GetPolicies() {
        return this.http.get<PolicyResponse[]>(this.baseUrl);
    }

    /** Create a policy */
    public CreatePolicy(request: CreatePolicyRequest) {
        return this.http.post<PolicyResponse>(this.baseUrl, request);
    }

    /** update a policy */
    public UpdatePolicy(policyId: string, request: UpdatePolicyRequest) {
        return this.http.put<PolicyResponse>(`${this.baseUrl}/${policyId}`, request);
    }

    /** Delete a policy */
    public DeletePolicy(policyId: string) {
        return this.http.delete<void>(`${this.baseUrl}/${policyId}`);
    }
}
