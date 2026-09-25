#include "WiFiManager.h"
#include "DeviceManager.h"
#include "WebSocketClient.h"
#include "MessageSigner.h"

#include <ArduinoJson.h>
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

// Generated per board by the DeviceKeyTool
const String DeviceKey = "";
MessageSigner messageSigner(DeviceKey);

void setup() {
  Serial.begin(115200);

  Serial.println();
  Serial.print("MAC address: ");
  Serial.println(WiFi.macAddress());

  // wait until the ESP8266 connected to WiFi
  wifiManager.begin();
  while (!wifiManager.isConnected()) {
    wifiManager.handleClient();
  }

  webSocketClient.begin();
  webSocketClient.onMessageReceived.subscribe([](String message){
    // sync the clock with server
    if (message.startsWith("{")) {
      JsonDocument doc;

      if (deserializeJson(doc, message) == DeserializationError::Ok && doc["serverTime"].is<uint64_t>()) {
        messageSigner.syncTime(doc["serverTime"].as<uint64_t>());
        Serial.println("Clock synced with the server");
      }

      return;
    }

    deviceManager.processMessage(message);
  });

  deviceManager.onTelemetrySent.subscribe([](String telemetry) {
    if (!messageSigner.isSynced()) {
      return;
    }

    Serial.print("heap before sign: ");
    Serial.println(ESP.getFreeHeap());

    String message = messageSigner.signMessage(WiFi.macAddress(), deviceManager.getType(), telemetry);

    Serial.print("signed, length ");
    Serial.print(message.length());
    Serial.print(", heap ");
    Serial.println(ESP.getFreeHeap());

    webSocketClient.sendMessage(message);

    Serial.println("sent");
  });
}

void loop() {
  webSocketClient.loop();
  deviceManager.loop();
}

