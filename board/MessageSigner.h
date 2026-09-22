#ifndef MESSAGE_SIGNER_H
#define MESSAGE_SIGNER_H

#include <Arduino.h>

class MessageSigner {
  public:
    MessageSigner(String key);
    void syncTime(uint64_t serverTimeMs);
    bool isSynced();
    String signMessage(String macAddress, String deviceType, String data);

  private:
    String hashMessage(String message);
    static String normalizeMacAddress(String macAddress);

    String _key;
    uint64_t _serverTimeMs = 0;
    unsigned long _millisAtSync = 0;
    bool _isSynced = false;
};

#endif
