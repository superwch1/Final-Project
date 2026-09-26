#ifndef DEVICE_MANAGER_H
#define DEVICE_MANAGER_H

#include "Event.h"
#include <Arduino.h>
#include "DHT.h"

enum DeviceType {
  LightSensor,
  TempAndHumidSensor,
  LedActuator,
  FanActuator,
  Unknown
};

class DeviceManager {
  public:
    /// Set up the pins and detects the device type
    DeviceManager(int typePin1, int typePin2, int inputPin, int outputPin);

    /// Get the device type name
    String getType(); 

    /// Process the message and Switches the actuator on or off
    void processMessage(String message);

    /// Send the telemetry in JSON
    Event<String> onTelemetrySent;

    /// Raise onTelemetrySent per telemetry interval
    void loop();

  private:
    int _typePin1;
    int _typePin2;
    int _inputPin;
    int _outputPin;
    DeviceType _type;
    unsigned long _lastTelemetry = 0;
    DHT _dht;
};

#endif