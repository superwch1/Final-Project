using Backend.Enumerations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    [Index(nameof(ActuatorMacAddress), IsUnique = true)]
    [Index(nameof(SensorMacAddress))]
    public record Policy
    {
        public required Guid Id { get; set; }

        [MaxLength(12)]
        public required string SensorMacAddress { get; set; }

        [MaxLength(12)]
        public required string ActuatorMacAddress { get; set; }

        public required SensorReading Reading { get; set; }

        public required Comparison Comparison { get; set; }

        public required double Threshold { get; set; }

        public required ActuatorState ActuatorState { get; set; }

        public required bool IsEnabled { get; set; }

        public Device? Sensor { get; set; }

        public Device? Actuator { get; set; }
    }
}
