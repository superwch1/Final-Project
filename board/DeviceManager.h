#ifndef DEVICE_MANAGER_H
#define DEVICE_MANAGER_H

#include "Event.h"
#include <Arduino.h>

enum DeviceType {
  LightSensor,
  TempAndHumidSensor,
  LedActuator,
  FanActuator,
  Unknown
};

class DeviceManager {
  public:
    DeviceManager(int typePin1, int typePin2, int outputPin);
    String getType(); 
    void processMessage(String message);
    Event<String> onTelemetrySent;
    void loop();

  private:
    int _typePin1;
    int _typePin2;
    int _outputPin;
    DeviceType _type;
    unsigned long _lastTelemetry = 0;
};

#endif