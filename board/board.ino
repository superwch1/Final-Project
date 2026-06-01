#include "WiFiManager.h"
#include "RoleDetector.h"
#include <ESP8266WiFi.h>
#include <ESP8266HTTPClient.h>

const int rolePin1 = D6;
const int rolePin2 = D7;
RoleDetector roleDetector(rolePin1, rolePin2);

const int successPin = D4;
const int loadingPin = D0;
WiFiManager wifiManager(successPin, loadingPin);

void setup() {
  Serial.begin(115200);
  roleDetector.detectRole();
  wifiManager.begin();

  pinMode(D1, OUTPUT);
}

void loop() {
  
  if (wifiManager.isConnected()) {

    WiFiClient client;
    HTTPClient http;

    String url = "http://192.168.1.7:5000/device/state?longPolling=true";

    http.begin(client, url);
    http.addHeader("X-Mac-Address", WiFi.macAddress());
    http.setTimeout(true ? 30000 : 5000);   // long hold needs a long read timeout

    int statusCode = http.GET(); 
    if (statusCode == 200) {
      String body = http.getString();   // bare JSON boolean: "true" or "false"
      body.trim();

      bool result = (body == "true") ? true : false;
      if (result == true) {
        digitalWrite(D1, HIGH);
      } else {
        digitalWrite(D1, LOW);
      }
    }

    http.end();

  } else {
    wifiManager.handleClient();
  }

  delay(100);
}

