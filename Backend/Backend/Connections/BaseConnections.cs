using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace Backend.Connections
{
    public abstract class BaseConnections
    {
        private readonly ConcurrentDictionary<Guid, WebSocket> _webSocketByConnectionId = new();


        /// <summary>
        /// Handles a text message received from a client
        /// </summary>
        protected abstract Task OnMessageReceived(Guid connectionId, string message);

        /// <summary>
        /// Sends a message to a connected client
        /// </summary>
        protected async Task SendMessageAsync(Guid connectionId, string message, CancellationToken cancellationToken)
        {
            if (_webSocketByConnectionId.TryGetValue(connectionId, out WebSocket? webSocket) && webSocket != null && webSocket.State == WebSocketState.Open)
            {
                await SendTextAsync(webSocket, message, cancellationToken);
            }
        }

        /// <summary>
        /// Sends a message to every connected client
        /// </summary>
        protected async Task SendMessageToAllAsync(string message, CancellationToken cancellationToken)
        {
            foreach (WebSocket webSocket in _webSocketByConnectionId.ToArray().Select(x => x.Value))
            {
                await SendTextAsync(webSocket, message, cancellationToken);
            }
        }

        /// <summary>
        /// Registers the socket, sends initial messages, then receives messages until the client closes.
        /// </summary>
        protected virtual async Task Echo(Guid connectionId, WebSocket webSocket, IEnumerable<string> initialMessages, CancellationToken cancellationToken)
        {
            try
            {
                _webSocketByConnectionId.AddOrUpdate(connectionId, webSocket, (_, _) => webSocket);
                foreach (string initialMessage in initialMessages)
                {
                    await SendTextAsync(webSocket, initialMessage, cancellationToken);
                }

                byte[] buffer = new byte[1024 * 4];
                WebSocketReceiveResult receivedResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                while (!receivedResult.CloseStatus.HasValue)
                {
                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, receivedResult.Count);
                    await OnMessageReceived(connectionId, receivedMessage);
                    receivedResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                }

                await webSocket.CloseAsync(receivedResult.CloseStatus.Value, receivedResult.CloseStatusDescription, cancellationToken);
            }
            finally
            {
                _webSocketByConnectionId.TryRemove(connectionId, out _);
            }
        }

        /// <summary>
        /// Sends a UTF-8 text if the socket is still open.
        /// </summary>
        private static async Task SendTextAsync(WebSocket webSocket, string message, CancellationToken cancellationToken)
        {
            if (webSocket.State == WebSocketState.Open)
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, cancellationToken);
            }
        }
    }
}
