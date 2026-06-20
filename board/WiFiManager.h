#ifndef WIFI_MANAGER_H
#define WIFI_MANAGER_H

#include <Arduino.h>
#include <ESP8266WebServer.h>  

class WiFiManager {
  public:
    WiFiManager(int successPin, int loadingPin, String hotspotSsid);
    void begin();    
    void handleClient();  
    bool isConnected();

  private:
    void handleIndex();
    void handleConnect();

    ESP8266WebServer _server;
    int _successPin;
    int _loadingPin;
    String _hotspotSsid;
};

#endif