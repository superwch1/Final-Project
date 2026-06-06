using System.Net.WebSockets;

namespace Backend.Connections
{
    public class DashboardConnections : BaseConnections
    {
        public async Task DashboardEcho(WebSocket webSocket, IEnumerable<string> initialMessages, CancellationToken cancellationToken)
        {
            Guid connectionId = Guid.NewGuid();
            try
            {
                await Echo(connectionId, webSocket, initialMessages, cancellationToken);
            }
            finally
            {
            }
        }

        protected override Task OnMessageReceived(Guid connectionId, string message)
        {
            return Task.CompletedTask;
        }

        public async Task NotifyTelemetryChanged(string message, CancellationToken cancellationToken)
        {
            await SendMessageToAllAsync(message, cancellationToken);
        }
    }
}
