#include "DeviceManager.h"
#include <ArduinoJson.h>
#include <ESP8266WiFi.h>

constexpr unsigned long TelemetryInterval = 1000;  // ms

DeviceManager::DeviceManager(int typePin1, int typePin2, int inputPin, int outputPin)
  : _typePin1(typePin1), _typePin2(typePin2),_inputPin(inputPin), _outputPin(outputPin), _dht(_outputPin, DHT11) {

  pinMode(_typePin1, INPUT_PULLUP);
  pinMode(_typePin2, INPUT_PULLUP);
  pinMode(_inputPin, INPUT);
  pinMode(_outputPin, OUTPUT);

  // light sensor: both typePin1 and typePin2 is connected to the ground pin
  if (digitalRead(_typePin1) == LOW && digitalRead(_typePin2) == LOW) {
    _type = LightSensor;
  } 
  // temp and humid sensor: only typePin1 is connected to the ground pin 
  else if (digitalRead(_typePin1) == LOW && digitalRead(_typePin2) == HIGH) {
    _type = TempAndHumidSensor;
    _dht.begin();
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

    case TempAndHumidSensor:  
      return "TempAndHumidSensor";

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
  doc["macAddress"] = WiFi.macAddress();

  if (_type == LedActuator || _type == FanActuator) {
    doc["actuatorState"] = digitalRead(_outputPin) == HIGH ? "On" : "Off";  
  }
  else if (_type == LightSensor) {.
    int rawReading = analogRead(A0);
    int lightReading = constrain(map(rawReading, 0, 1023, 100, 0), 0, 100);
    doc["lightReading"] = lightReading;
  }
  else if (_type == TempAndHumidSensor) {
    doc["temperatureReading"] = _dht.readTemperature();
    doc["humidityReading"] = _dht.readHumidity();
  }
  
  String telemetry;
  serializeJson(doc, telemetry);
  onTelemetrySent.emit(telemetry);
}
