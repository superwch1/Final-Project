#ifndef WIFI_MANAGER_H
#define WIFI_MANAGER_H

#include <Arduino.h>
#include <ESP8266WebServer.h>  

class WiFiManager {
  public:
    /// Store the status LED pins and hotspot name
    WiFiManager(int successPin, int loadingPin, String hotspotSsid);

    /// Start the setup hotspot and web page for entering WiFi details
    void begin();    

    /// Serve pending requests to the setup web page
    void handleClient();  

    /// Return true once the board is connected to WiFi
    bool isConnected();

  private:
    /// Serve the form for entering the WiFi name and password
    void handleIndex();

    /// Connect to the WiFi network
    void handleConnect();

    ESP8266WebServer _server;
    int _successPin;
    int _loadingPin;
    String _hotspotSsid;
};

#endif