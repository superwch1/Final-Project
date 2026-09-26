using Backend.Connections;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;

namespace Backend.Controllers
{
    [Route("[controller]")]
    public class DashboardController : Controller
    {
        private readonly IConnectionMediator _connectionMediator;

        public DashboardController(IConnectionMediator connectionMediator)
        {
            _connectionMediator = connectionMediator;
        }


        /// <summary>
        /// Accepts a dashboard WebSocket connection
        /// </summary>
        [HttpGet("ws")]
        public async Task WebSocket(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Connected dashboard");
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await _connectionMediator.DashboardEcho(webSocket, cancellationToken);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
    }
}
