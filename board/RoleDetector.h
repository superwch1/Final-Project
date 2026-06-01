#ifndef ROLE_DETECTOR_H
#define ROLE_DETECTOR_H

#include <Arduino.h>

enum BoardRole {
  LIGHT_SENSOR,
  TEMP_SENSOR,
  LED_ACTUATOR,
  FAN_ACTUATOR,
  UNKNOWN
};

class RoleDetector {
  public:
    RoleDetector(int rolePin1, int rolePin2);

    void detectRole();        
    BoardRole getRole();  

  private:
    int _rolePin1;
    int _rolePin2;
    BoardRole _role;
};

#endif