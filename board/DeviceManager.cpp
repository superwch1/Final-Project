#include "DeviceManager.h"
#include <ArduinoJson.h>
#include <ESP8266WiFi.h>

constexpr unsigned long TelemetryInterval = 5000;  // ms

DeviceManager::DeviceManager(int typePin1, int typePin2, int outputPin)
  : _typePin1(typePin1), _typePin2(typePin2), _outputPin(outputPin) {

  pinMode(_typePin1, INPUT_PULLUP);
  pinMode(_typePin2, INPUT_PULLUP);
  pinMode(_outputPin, OUTPUT);

  // light sensor: both typePin1 and typePin2 is connected to the ground pin
  if (digitalRead(_typePin1) == LOW && digitalRead(_typePin2) == LOW) {
    _type = LightSensor;
  } 
  // temp sensor: only typePin1 is connected to the ground pin 
  else if (digitalRead(_typePin1) == LOW && digitalRead(_typePin2) == HIGH) {
    _type = TempSensor;
  } 
  // led actuator: only typePin2 is connected to the ground pin 
  else if (digitalRead(_typePin1) == HIGH && digitalRead(_typePin2) == LOW) {
    _type = LedActuator;
  } 
  // fan actuator: both typePin1 and typePin2 is not connected to the ground pin
  else if (digitalRead(_typePin1) == HIGH && digitalRead(_typePin2) == HIGH) {
    _type = FanActuator;
  } 
  else {
    _type = Unknown;
  }
}

String DeviceManager::getType() {
  switch (_type) {
    case LightSensor: 
      return "LightSensor";

    case TempSensor:  
      return "TempSensor";

    case LedActuator: 
      return "LedActuator";

    case FanActuator: 
      return "FanActuator";

    default:          
      return "Unknown";
  }
  return "Unknown";
}

void DeviceManager::processMessage(String message) {

  if (_type == LedActuator || _type == FanActuator) {
    if (message == "On") {
      digitalWrite(_outputPin, HIGH);
    } else if (message == "Off") {
      digitalWrite(_outputPin, LOW);
    }
  }
}

void DeviceManager::loop() {

  if (millis() - _lastTelemetry < TelemetryInterval) {
    return;
  }
  _lastTelemetry = millis();

  JsonDocument doc;
  doc["deviceType"] = getType(); 
  doc["wifiSignal"] = WiFi.RSSI(); 
  doc["freeHeap"] = ESP.getFreeHeap();

  if (_type == LedActuator || _type == FanActuator) {
    doc["actuatorState"] = digitalRead(_outputPin) == HIGH ? "On" : "Off";  
  }
  
  String out;
  serializeJson(doc, out);
  onTelemetrySent.emit(out);
}
