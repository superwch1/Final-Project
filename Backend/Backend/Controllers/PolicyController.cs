using Backend.Connections;
using Backend.Enumerations;
using Backend.Models;
using Backend.Repositories;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class PolicyController : ControllerBase
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly IDeviceRepository _deviceRepository;
        private readonly IRoomRepository _roomRepository;
        private readonly IDeviceStore _deviceStore;
        private readonly IDeviceKeyService _deviceKeyService;

        public PolicyController(
            IPolicyRepository policyRepository,
            IDeviceRepository deviceRepository,
            IRoomRepository roomRepository,
            IDeviceStore deviceStore,
            IDeviceKeyService deviceKeyService)
        {
            _policyRepository = policyRepository;
            _deviceRepository = deviceRepository;
            _roomRepository = roomRepository;
            _deviceStore = deviceStore;
            _deviceKeyService = deviceKeyService;
        }


        /// <summary>
        /// Returns every policy owned by the account.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PolicyResponse>>> GetAll(CancellationToken cancellationToken)
        {
            if (!TryGetAccountId(out Guid accountId))
            {
                return Unauthorized();
            }

            List<Policy> policies = await _policyRepository.FindByAccountAsync(accountId, cancellationToken);
            return Ok(policies.Select(PolicyResponse.FromPolicy));
        }


        /// <summary>
        /// Creates a policy
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PolicyResponse>> Create(CreatePolicyRequest request, CancellationToken cancellationToken)
        {
            string sensorMacAddress = _deviceKeyService.NormalizeMacAddress(request.SensorMacAddress);
            string actuatorMacAddress = _deviceKeyService.NormalizeMacAddress(request.ActuatorMacAddress);

            if (sensorMacAddress == actuatorMacAddress)
            {
                return BadRequest("A device cannot drive itself.");
            }

            Device? sensor = await FindOwnedDeviceAsync(sensorMacAddress, cancellationToken);
            Device? actuator = await FindOwnedDeviceAsync(actuatorMacAddress, cancellationToken);
            if (sensor is null || actuator is null)
            {
                return NotFound();
            }

            if (!sensor.DeviceType.IsSensor())
            {
                return BadRequest("Device is not a sensor.");
            }

            if (!actuator.DeviceType.IsActuator())
            {
                return BadRequest("Device is not an actuator.");
            }

            if (!Reports(sensor, request.Reading))
            {
                return BadRequest($"That sensor does not report {request.Reading}.");
            }

            Policy policy = new()
            {
                Id = Guid.NewGuid(),
                SensorMacAddress = sensorMacAddress,
                ActuatorMacAddress = actuatorMacAddress,
                Reading = request.Reading,
                Comparison = request.Comparison,
                Threshold = request.Threshold,
                ActuatorState = request.ActuatorState,
                IsEnabled = true
            };

            if (!await _policyRepository.TryAddAsync(policy, cancellationToken))
            {
                return Conflict("That actuator is already driven by a policy.");
            }

            _deviceStore.ForgetPolicies(sensorMacAddress);

            policy.Sensor = sensor;
            policy.Actuator = actuator;

            return Ok(PolicyResponse.FromPolicy(policy));
        }



        /// <summary>
        /// Update the policy
        /// </summary>
        [HttpPut("{policyId}")]
        public async Task<ActionResult<PolicyResponse>> Update(Guid policyId, UpdatePolicyRequest request, CancellationToken cancellationToken)
        {
            Policy? policy = await FindPolicyIfOwnedAsync(policyId, cancellationToken);
            if (policy is null)
            {
                return NotFound();
            }

            if ((policy.Sensor is not null) && !Reports(policy.Sensor, request.Reading))
            {
                return BadRequest($"That sensor does not report {request.Reading}.");
            }

            policy.Reading = request.Reading;
            policy.Comparison = request.Comparison;
            policy.Threshold = request.Threshold;
            policy.ActuatorState = request.ActuatorState;
            policy.IsEnabled = request.IsEnabled;

            await _policyRepository.UpdateAsync(policy, cancellationToken);
            _deviceStore.ForgetPolicies(policy.SensorMacAddress);

            return Ok(PolicyResponse.FromPolicy(policy));
        }


        /// <summary>
        /// Deletes an policy
        /// </summary>
        [HttpDelete("{policyId}")]
        public async Task<ActionResult> Delete(Guid policyId, CancellationToken cancellationToken)
        {
            Policy? policy = await FindPolicyIfOwnedAsync(policyId, cancellationToken);

            if (policy is null)
            {
                return NotFound();
            }

            await _policyRepository.DeleteAsync(policy, cancellationToken);
            _deviceStore.ForgetPolicies(policy.SensorMacAddress);

            return NoContent();
        }


        /// <summary>
        /// Returns true if the sensor's device type reports the given reading
        /// </summary>
        private static bool Reports(Device sensor, SensorReading reading)
        {
            return reading switch
            {
                SensorReading.Light => sensor.DeviceType == DeviceType.LightSensor,
                SensorReading.Temperature or SensorReading.Humidity => sensor.DeviceType == DeviceType.TempAndHumidSensor,
                _ => false
            };
        }


        /// <summary>
        /// Loads a device only if it is paired to a room owned by the account
        /// </summary>
        private async Task<Device?> FindOwnedDeviceAsync(string macAddress, CancellationToken cancellationToken)
        {
            if (!TryGetAccountId(out Guid accountId))
            {
                return null;
            }

            Device? device = await _deviceRepository.FindByMacAddressAsync(macAddress, cancellationToken);

            if (device is null)
            {
                return null;
            }

            if (device.RoomId is null)
            {
                return null;
            }

            Room? room = await _roomRepository.FindByIdAsync(device.RoomId.Value, cancellationToken);

            return (room?.AccountId == accountId) ? device : null;
        }

        /// <summary>
        /// Loads a policy only if its sensor is owned by the account
        /// </summary>
        private async Task<Policy?> FindPolicyIfOwnedAsync(Guid policyId, CancellationToken cancellationToken)
        {
            Policy? policy = await _policyRepository.FindByIdAsync(policyId, cancellationToken);
            if (policy is null)
            {
                return null;
            }

            return (await FindOwnedDeviceAsync(policy.SensorMacAddress, cancellationToken) is null) ? null : policy;
        }

        /// <summary>
        /// Reads the account ID from the user's claims
        /// </summary>
        private bool TryGetAccountId(out Guid accountId)
        {
            return Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out accountId);
        }
    }
}
