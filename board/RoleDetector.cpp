#include "RoleDetector.h"

RoleDetector::RoleDetector(int rolePin1, int rolePin2)
  : _rolePin1(rolePin1), _rolePin2(rolePin2), _role(UNKNOWN) {
}

void RoleDetector::detectRole() {
  pinMode(_rolePin1, INPUT_PULLUP);
  pinMode(_rolePin2, INPUT_PULLUP);

  // light sensor: both rolePin1 and rolePin2 is connected to the ground pin
  if (digitalRead(_rolePin1) == LOW && digitalRead(_rolePin2) == LOW) {
    _role = LIGHT_SENSOR;
  } 
  // temp sensor: only rolePin1 is connected to the ground pin 
  else if (digitalRead(_rolePin1) == LOW && digitalRead(_rolePin2) == HIGH) {
    _role = TEMP_SENSOR;
  } 
  // led actuator: only rolePin2 is connected to the ground pin 
  else if (digitalRead(_rolePin1) == HIGH && digitalRead(_rolePin2) == LOW) {
    _role = LED_ACTUATOR;
  } 
  // fan actuator: both rolePin1 and rolePin2 is not connected to the ground pin
  else if (digitalRead(_rolePin1) == HIGH && digitalRead(_rolePin2) == HIGH) {
    _role = FAN_ACTUATOR;
  } 
}

BoardRole RoleDetector::getRole() {
  return _role;
}