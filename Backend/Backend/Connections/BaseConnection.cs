using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace Backend.Connections
{
    public abstract class BaseConnection
    {
        private readonly ConcurrentDictionary<Guid, WebSocket> _webSocketByConnectionId = new();


        protected abstract Task OnMessageReceived(Guid connectionId, string message);

        protected async Task SendMessage(IEnumerable<Guid> connectionIds, string message, CancellationToken cancellationToken)
        {
            foreach (Guid connectionId in connectionIds)
            {
                await SendMessage(connectionId, message, cancellationToken);
            }
        }

        protected async Task SendMessage(Guid connectionId, string message, CancellationToken cancellationToken)
        {
            if (_webSocketByConnectionId.TryGetValue(connectionId, out WebSocket? webSocket) &&
                webSocket != null && webSocket.State == WebSocketState.Open)
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, cancellationToken);
            }
        }

        protected virtual async Task Echo(Guid connectionId, WebSocket webSocket, CancellationToken cancellationToken, string initialMessage = "")
        {
            try
            {
                _webSocketByConnectionId.AddOrUpdate(connectionId, webSocket, (_, _) => webSocket);
                if (!string.IsNullOrEmpty(initialMessage))
                    await SendMessage(connectionId, initialMessage, cancellationToken);

                byte[] buffer = new byte[1024 * 4];
                WebSocketReceiveResult receivedResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                do
                {
                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, receivedResult.Count);
                    await OnMessageReceived(connectionId, receivedMessage);
                    receivedResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                }
                while (!receivedResult.CloseStatus.HasValue);

                await webSocket.CloseAsync(receivedResult.CloseStatus.Value, receivedResult.CloseStatusDescription, cancellationToken);
            }
            finally
            {
                _webSocketByConnectionId.TryRemove(connectionId, out _);
            }
        }
    }
}
