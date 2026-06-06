using Backend.Connections;
using Backend.Enumerations;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("[controller]")]
    public class DashboardController : Controller
    {
        private readonly ConnectionMediator _connectionMediator;

        public DashboardController(ConnectionMediator connectionMediator)
        {
            _connectionMediator = connectionMediator;
        }


        [HttpGet("{macAddress}/{actuatorState}")]
        public async Task<ActionResult> SetActuatorState(string macAddress, ActuatorState actuatorState, CancellationToken cancellationToken)
        {
            await _connectionMediator.SetActuatorState(macAddress, actuatorState, cancellationToken);
            return Ok($"Message sent to device {macAddress}");
        }
    }
}
