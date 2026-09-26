#ifndef MESSAGE_SIGNER_H
#define MESSAGE_SIGNER_H

#include <Arduino.h>

class MessageSigner {
  public:
    /// Store the device key used to sign messages
    MessageSigner(String key);

    /// Sync the clock time with server
    void syncTime(uint64_t serverTimeMs);

    /// Return true once the time is sync
    bool isSynced();

    /// Wrap the data in JSON with a timestamp and signature
    String signMessage(String macAddress, String deviceType, String data);

  private:
    /// Hash the message to create signature
    String hashMessage(String message);

    /// Remove separators from a MAC address and upper-cases it
    static String normalizeMacAddress(String macAddress);

    String _key;
    uint64_t _serverTimeMs = 0;
    unsigned long _millisAtSync = 0;
    bool _isSynced = false;
};

#endif
