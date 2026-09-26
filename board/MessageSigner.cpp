#include "MessageSigner.h"
#include <ArduinoJson.h>
#include <bearssl/bearssl_hmac.h>

MessageSigner::MessageSigner(String key)
  : _key(key) {
}

void MessageSigner::syncTime(uint64_t serverTimeMs) {
  _serverTimeMs = serverTimeMs;
  _millisAtSync = millis();
  _isSynced = true;
}

bool MessageSigner::isSynced() {
  return _isSynced;
}

String MessageSigner::signMessage(String macAddress, String deviceType, String data) {

  uint64_t timestamp = _serverTimeMs + (uint64_t)(millis() - _millisAtSync);

  String messageWithTimestamp = normalizeMacAddress(macAddress) + "|" + deviceType + "|" + String((unsigned long long)timestamp) + "|" + data;

  JsonDocument json;
  json["timestamp"] = timestamp;
  json["data"] = serialized(data);
  json["signature"] = hashMessage(messageWithTimestamp);

  String message;
  serializeJson(json, message);
  return message;
}

String MessageSigner::hashMessage(String message) {
  br_hmac_key_context keyContext;
  br_hmac_key_init(&keyContext, &br_sha256_vtable, _key.c_str(), _key.length());

  br_hmac_context hmacContext;
  br_hmac_init(&hmacContext, &keyContext, 0);
  br_hmac_update(&hmacContext, message.c_str(), message.length());

  uint8_t digest[32];
  br_hmac_out(&hmacContext, digest);

  String hex;
  hex.reserve(sizeof(digest) * 2);

  for (size_t i = 0; i < sizeof(digest); i++) {
    char pair[3];
    snprintf(pair, sizeof(pair), "%02X", digest[i]);
    hex += pair;
  }

  return hex;
}

String MessageSigner::normalizeMacAddress(String macAddress) {
  macAddress.replace(":", "");
  macAddress.replace("-", "");
  macAddress.toUpperCase();
  return macAddress;
}
