import { inject, Injectable, signal } from "@angular/core";
import { BaseTelemetry } from "../models/telemetry/base-telemetry.interface";
import { AuthService } from "./auth.service";

@Injectable({
    providedIn: 'root'
})

export class TelemetryService {
    private readonly authService = inject(AuthService);
    private readonly reconnectDelay = 5000;

    private webSocket: WebSocket | null = null;
    private reconnectTimer: ReturnType<typeof setTimeout> | null = null;
    private shouldReconnect = false;

    private readonly latest = signal<Record<string, BaseTelemetry>>({});

    public readonly telemetry = this.latest.asReadonly();

    /** Open the WebSocket connection */
    public connect(): void {
        this.shouldReconnect = true;
        this.open();
    }

    /** Close the WebSocket connection */
    public disconnect(): void {
        this.shouldReconnect = false;

        if (this.reconnectTimer !== null) {
            clearTimeout(this.reconnectTimer);
            this.reconnectTimer = null;
        }

        this.webSocket?.close();
        this.webSocket = null;
    }

    /** Remove separators from a MAC address and upper-cases it */
    public static normalizeMacAddress(macAddress: string): string {
        return macAddress.replace(/[:-]/g, '').toUpperCase();
    }

    /** Open the WebSocket and signs in with the token */
    private open(): void {
        this.webSocket = new WebSocket('ws://192.168.1.7:5000/dashboard/ws');

        this.webSocket.onopen = () => this.webSocket?.send(this.authService.getToken() ?? '');

        this.webSocket.onmessage = (event) => {
            const telemetry = JSON.parse(event.data) as BaseTelemetry;

            this.latest.update(current => ({
                ...current,
                [TelemetryService.normalizeMacAddress(telemetry.macAddress)]: telemetry
            }));
        };

        this.webSocket.onclose = () => {
            if (!this.shouldReconnect || this.reconnectTimer !== null) {
                return;
            }

            this.reconnectTimer = setTimeout(() => {
                this.reconnectTimer = null;
                this.open();
            }, this.reconnectDelay);
        };
    }
}
