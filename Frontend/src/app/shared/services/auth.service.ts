import { Injectable, signal } from "@angular/core";
import { AccessTokenPayload } from "../models/access-token-payload.interface";

/** The localStorage key holding the access token */
const TokenStorageKey = 'accessToken';

@Injectable({
    providedIn: 'root'
})

export class AuthService {
    private readonly payload = signal<AccessTokenPayload | null>(AuthService.readPayload());

    /** Returns the stored access token, or null if there is none */
    public getToken(): string | null {
        return localStorage.getItem(TokenStorageKey);
    }

    /** Return true if the stored token has not expired */
    public isSignedIn(): boolean {
        
        // check whether the stored token is still valid
        const expiresAt = this.payload()?.exp ?? 0;
        return (expiresAt * 1000) > Date.now();
    }

    /** Return the account Id */
    public accountId(): string | null {
        return this.isSignedIn() ? (this.payload()?.sub ?? null) : null;
    }

    /** Return the account email */
    public email(): string | null {
        return this.isSignedIn() ? (this.payload()?.email ?? null) : null;
    }

    /** Return the account name */
    public name(): string | null {
        return this.isSignedIn() ? (this.payload()?.name ?? null) : null;
    }

    /** Store a access token and reads its payload */
    public setToken(accessToken: string): void {
        localStorage.setItem(TokenStorageKey, accessToken);
        this.payload.set(AuthService.readPayload());
    }

    /** Remove the stored access token */
    public signOut(): void {
        localStorage.removeItem(TokenStorageKey);
        this.payload.set(null);
    }

    /** Read the stored token payload, or null if it is missing */
    private static readPayload(): AccessTokenPayload | null {
        const segments = localStorage.getItem(TokenStorageKey)?.split('.');

        if (segments?.length !== 3) {
            return null;
        }

        try {
            const base64 = segments[1].replace(/-/g, '+').replace(/_/g, '/');
            const bytes = Uint8Array.from(atob(base64), character => character.charCodeAt(0));

            return JSON.parse(new TextDecoder().decode(bytes)) as AccessTokenPayload;
        } catch {
            return null;
        }
    }
}
