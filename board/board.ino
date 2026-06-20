#include "WiFiManager.h"
#include "DeviceManager.h"
#include "WebSocketClient.h"

#include <ESP8266WiFi.h>
#include <ESP8266HTTPClient.h>

const int typePin1 = D6;
const int typePin2 = D7;
const int inputPin = A0;
const int outputPin = D1;
DeviceManager deviceManager(typePin1, typePin2, inputPin, outputPin);

const int successPin = D4;
const int loadingPin = D0;
WiFiManager wifiManager(successPin, loadingPin, deviceManager.getType());

const String host = "192.168.1.7";
const int port = 5000;
const String path = "/device/ws?macAddress=" + WiFi.macAddress() + "&deviceType=" + deviceManager.getType();
WebSocketClient webSocketClient(host, port, path);

void setup() {
  Serial.begin(115200);

  // wait until the ESP8266 connected to WiFi
  wifiManager.begin();
  while (!wifiManager.isConnected()) {
    wifiManager.handleClient();
  }

  webSocketClient.begin();
  webSocketClient.onMessageReceived.subscribe([](String message){
    deviceManager.processMessage(message);
  });
  
  deviceManager.onTelemetrySent.subscribe([](String telemetry) {
    webSocketClient.sendMessage(telemetry);
  });
}

void loop() {
  webSocketClient.loop();
  deviceManager.loop();
}

