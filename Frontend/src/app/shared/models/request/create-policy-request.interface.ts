import { ActuatorState } from "../../enumerations/actuator-state.enum";
import { Comparison } from "../../enumerations/comparison.enum";
import { SensorReading } from "../../enumerations/sensor-reading.enum";

export interface CreatePolicyRequest {
  sensorMacAddress: string;
  actuatorMacAddress: string;
  reading: SensorReading;
  comparison: Comparison;
  threshold: number;
  actuatorState: ActuatorState;
}
