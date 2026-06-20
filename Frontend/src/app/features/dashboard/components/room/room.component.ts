import { Component, inject, OnDestroy, OnInit, Signal, signal, WritableSignal } from '@angular/core';
import { BaseTelemetry } from '../../models/telemetry/base-telemetry.interface';
import { DeviceType } from '../../enumerations/device-type.enum';
import { LedTelemetry } from '../../models/telemetry/led-telemetry.interface';
import { DeviceApiService } from '../../services/device-api.service';
import { ActuatorState } from '../../enumerations/actuator-state.enum';
import { LightTelemetry } from '../../models/telemetry/light-telemetry.interface';
import { TempAndHumidTelemetry } from '../../models/telemetry/temp-and-humid-telemetry.interface';

@Component({
  selector: 'app-room',
  imports: [],
  templateUrl: './room.component.html',
  styleUrl: './room.component.css'
})
export class RoomComponent implements OnInit, OnDestroy {

  private webSocket: WebSocket | null = null;
  private shouldReconnect = true;
  private reconnectTimer: ReturnType<typeof setTimeout> | null = null;
  private readonly reconnectDelay = 5000;

  private deviceApiService = inject(DeviceApiService);

  protected readonly DeviceType = DeviceType;
  protected readonly ActuatorState = ActuatorState;

  // private lookup so we can find a device's signal by MAC in O(1)
  private readonly signalByMacAddress = new Map<string, WritableSignal<BaseTelemetry>>();

  // outer signal: the list the template iterates. Reference changes ONLY on add.
  protected readonly devices = signal<{ mac: string; data: Signal<BaseTelemetry> }[]>([]);

  ngOnInit(): void {
    this.startWebSocket();
  }

  ngOnDestroy(): void {
    this.shouldReconnect = false; // stop the retry loop
    if (this.reconnectTimer) clearTimeout(this.reconnectTimer);
    this.webSocket?.close();
  }

  private startWebSocket() {
    this.webSocket = new WebSocket('ws://192.168.1.7:5000/dashboard/ws');

    this.webSocket.onopen = () => console.log("Connected");
    this.webSocket.onclose = () => {
      if (!this.shouldReconnect || this.reconnectTimer !== null) {  // don't stack timers
        return;
      }

      console.log(`Reconnecting in ${this.reconnectDelay / 1000}s…`);
      this.reconnectTimer = setTimeout(() => {
        this.reconnectTimer = null;
        this.startWebSocket();
      }, this.reconnectDelay);
    };

    this.webSocket.onmessage = (event) => {
      const telemetry = JSON.parse(event.data) as BaseTelemetry;
      this.updateTelemetry(telemetry);
    };
  }

  private updateTelemetry(telemetry: BaseTelemetry): void {
    const existingDeviceSignal = this.signalByMacAddress.get(telemetry.macAddress);

    if (existingDeviceSignal) {
      // EXISTING device → update only its own signal. Outer list is untouched,
      // so @for does NOT re-run; only this section's bindings refresh.
      existingDeviceSignal.set(telemetry);
    } else {
      // NEW device → create its signal and rebuild the list (structural change).
      const deviceSignal = signal(telemetry);
      this.signalByMacAddress.set(telemetry.macAddress, deviceSignal);
      this.devices.update(existingDevices => [...existingDevices, { mac: telemetry.macAddress, data: deviceSignal.asReadonly() }]);
    }
  }

  asLed(t: BaseTelemetry): LedTelemetry {
    return t as LedTelemetry;
  }

  asLight(t: BaseTelemetry): LightTelemetry {
    return t as LightTelemetry;
  }

  asTempAndHumid(t: BaseTelemetry): TempAndHumidTelemetry {
    return t as TempAndHumidTelemetry;
  }

  protected setActuatorState(macAddress: string, actuatorState: ActuatorState): void {
    this.deviceApiService
      .SetActuatorState({macAddress: macAddress, actuatorState: actuatorState})
      .subscribe();
  }
}
