#include "WebSocketClient.h"

WebSocketClient::WebSocketClient(String host, int port, String path)
  : _host(host), _port(port), _path(path) {
}

void begin();    
    void loop();  
    bool sendMessage();

void WebSocketClient::begin() {
  _webSocket.begin(_host, _port, _path);
  _webSocket.onEvent([this](WStype_t type, uint8_t* payload, size_t length) {
    this->webSocketEvent(type, payload, length);
  });
  _webSocket.setReconnectInterval(5000);
}

void WebSocketClient::loop() {
  _webSocket.loop();
}

void WebSocketClient::sendMessage(String message) {
  _webSocket.sendTXT(message);
}

void WebSocketClient::webSocketEvent(WStype_t type, uint8_t * payload, size_t length) {
  switch (type) {
    case WStype_DISCONNECTED:
      Serial.println("[WSc] Disconnected!");
      break;

    case WStype_CONNECTED:
      _webSocket.sendTXT("[WSc] Connected");
      break;

    case WStype_TEXT:
      onMessageReceived.emit(String((char*)payload));
      break;

    case WStype_PING:
      Serial.println("[WSc] get ping");
      break;

    case WStype_PONG:
      Serial.println("[WSc] get pong");
      break;
  }
}