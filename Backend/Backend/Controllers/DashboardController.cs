using Backend.Connections;
using Backend.Enumerations;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("[controller]")]
    public class DashboardController : Controller
    {
        private readonly DeviceStore _deviceStore;

        public DashboardController(DeviceStore deviceStore)
        {
            _deviceStore = deviceStore;
        }


        [HttpGet("{macAddress}/{actuatorState}")]
        public async Task<ActionResult> UpdateActuatorState(string macAddress, ActuatorState actuatorState, CancellationToken cancellationToken)
        {
            await _deviceStore.SetActuatorState(macAddress, actuatorState, cancellationToken);
            return Ok($"Message sent to device {macAddress}");
        }
    }
}
