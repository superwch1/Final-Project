#ifndef WEBSOCKET_CLIENT_H
#define WEBSOCKET_CLIENT_H

#include "Event.h"
#include <WebSocketsClient.h>

class WebSocketClient {
  public:
    /// Store the backend address
    WebSocketClient(String host, int port, String path);

    /// Connect to the backend WebSocket
    void begin();    

    /// Process WebSocket traffic
    void loop();  

    /// Send a text message
    void sendMessage(String message);

    /// Process the text message received from backend
    Event<String> onMessageReceived;

  private:
    /// Handle connection events
    void webSocketEvent(WStype_t type, uint8_t * payload, size_t length);

    String _host;
    int _port;
    String _path;
    WebSocketsClient _webSocket;
};

#endif
