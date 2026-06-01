using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly DeviceManager _notifier;

        public DeviceController(DeviceManager notifier)
        {
            _notifier = notifier;
        }


        [HttpGet("register/{macAddress}")]
        public async Task<IActionResult> Register(string macAddress)
        {
            return Ok(macAddress);
        }


        [HttpGet("{macAddress}/state/{turnedOn}")]
        public async Task<IActionResult> Notify(string macAddress, bool turnedOn)
        {
            _notifier.NotifyActuatorState(macAddress, turnedOn);
            return Ok(macAddress);
        }


        // Long poll: held open until desired state changes or ~20s elapse.
        [HttpGet("state")]
        public async Task<IActionResult> State([FromHeader(Name = "X-Mac-Address")] string macAddress, CancellationToken cancellationToken, [FromQuery] bool longPolling = false)
        {
            bool state = longPolling 
                ? await _notifier.WaitForDeviceStateChangeAsync(macAddress, TimeSpan.FromSeconds(20), cancellationToken) 
                : _notifier.GetDeviceState(macAddress);

            return Ok(state);  
        }
    }
}
