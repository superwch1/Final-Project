import { ActuatorState } from "../../dashboard/enumerations/actuator-state.enum";
import { Comparison } from "../enumerations/comparison.enum";
import { SensorReading } from "../enumerations/sensor-reading.enum";

export interface UpdatePolicyRequest {
  reading: SensorReading;
  comparison: Comparison;
  threshold: number;
  actuatorState: ActuatorState;
  isEnabled: boolean;
}
