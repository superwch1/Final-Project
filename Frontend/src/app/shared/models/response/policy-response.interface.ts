import { ActuatorState } from "../../enumerations/actuator-state.enum";
import { Comparison } from "../../enumerations/comparison.enum";
import { SensorReading } from "../../enumerations/sensor-reading.enum";

export interface PolicyResponse {
  id: string;
  sensorMacAddress: string;
  sensorName: string;
  actuatorMacAddress: string;
  actuatorName: string;
  reading: SensorReading;
  comparison: Comparison;
  threshold: number;
  actuatorState: ActuatorState;
  isEnabled: boolean;
}
