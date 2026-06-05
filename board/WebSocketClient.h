#ifndef WEBSOCKET_CLIENT_H
#define WEBSOCKET_CLIENT_H

#include "Event.h"
#include <WebSocketsClient.h>

class WebSocketClient {
  public:
    WebSocketClient(String host, int port, String path);
    void begin();    
    void loop();  
    void sendMessage(String message);
    Event<String> onMessageReceived;

  private:
    void webSocketEvent(WStype_t type, uint8_t * payload, size_t length);
    String _host;
    int _port;
    String _path;
    WebSocketsClient _webSocket;
};

#endif
