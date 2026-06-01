#include "WiFiManager.h"
#include <ESP8266WiFi.h>

const IPAddress local_IP(192, 168, 4, 1);
const IPAddress gateway(192, 168, 4, 1);
const IPAddress subnet(255, 255, 255, 0);

const char* hotspotSsid = "ESP8266";

WiFiManager::WiFiManager(int successPin, int loadingPin)
  : _server(80), _successPin(successPin), _loadingPin(loadingPin) {
}

void WiFiManager::begin() {
  pinMode(_successPin, OUTPUT);
  pinMode(_loadingPin, OUTPUT);

  // onboard LED - LOW (turn on), HIGH (turn off)
  digitalWrite(_successPin, HIGH);
  digitalWrite(_loadingPin, LOW);

  WiFi.mode(WIFI_AP_STA);
  WiFi.softAPConfig(local_IP, gateway, subnet);
  WiFi.softAP(hotspotSsid);

  _server.on("/",        [this]() { handleIndex(); });
  _server.on("/connect", [this]() { handleConnect(); });
  _server.begin();
}

void WiFiManager::handleClient() {
  _server.handleClient();
}

bool WiFiManager::isConnected() {
  return WiFi.status() == WL_CONNECTED;
}

void WiFiManager::handleIndex() {
  String html = "<!DOCTYPE html><html>";
  html += "<head><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"></head>";
  html += "<body>";
  html += "<h3>Connect to local WiFi network</h3>";
  html += "<p>Chip Id: " + String(ESP.getChipId()) + "</p>";
  html += "<form action=\"/connect\" method=\"get\">";
  html += "<p>Network name (SSID):<br><input type=\"text\" name=\"ssid\"></p>";
  html += "<p>Password:<br><input type=\"password\" name=\"password\"></p>";
  html += "<p><input type=\"submit\" value=\"Connect\"></p>";
  html += "</form>";
  html += "</body></html>";
  _server.send(200, "text/html", html);
}

void WiFiManager::handleConnect() {
  String ssid = _server.arg("ssid");
  String password = _server.arg("password");

  String html = "<!DOCTYPE html><html><body>";
  html += "<h3>Connecting to " + ssid + " ...</h3>";
  html += "<p>Check the board led for connection status.</p>";
  html += "<p><a href=\"/\">Back</a></p>";
  html += "</body></html>";
  _server.send(200, "text/html", html);

  WiFi.begin(ssid, password);
  for (int i = 0; i < 20; i++) {
    if (WiFi.status() == WL_CONNECTED) {
      break;
    }

    digitalWrite(_loadingPin, LOW);   
    delay(250);
    digitalWrite(_loadingPin, HIGH);
    delay(250);
  }

  // onboard LED - LOW (turn on), HIGH (turn off)
  if (WiFi.status() == WL_CONNECTED) {
    digitalWrite(_loadingPin, HIGH);
    digitalWrite(_successPin, LOW);

    // switch to station mode after connecting to a local network
    // not allow other people to  access this device and add to a new room until being reset
    // WiFi.mode(WIFI_STA); , allow to send sccuess message
  } else {
    digitalWrite(_loadingPin, LOW);
    digitalWrite(_successPin, HIGH);
  }
}
