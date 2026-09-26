using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace Backend.Tests.Helpers
{
    public sealed class FakeWebSocket : WebSocket
    {
        private readonly Queue<string> _incoming;
        private readonly bool _stayOpen;
        private readonly TaskCompletionSource _closeRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private WebSocketState _state = WebSocketState.Open;
        private WebSocketCloseStatus? _closeStatus;

        public FakeWebSocket(IEnumerable<string>? incoming = null, bool stayOpen = false)
        {
            _incoming = new Queue<string>(incoming ?? []);
            _stayOpen = stayOpen;
        }

        public ConcurrentQueue<string> Sent { get; } = new();

        public override WebSocketCloseStatus? CloseStatus => _closeStatus;

        public override string? CloseStatusDescription => null;

        public override WebSocketState State => _state;

        public override string? SubProtocol => null;

        public void RequestClose()
        {
            _closeRequested.TrySetResult();
        }

        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            if (_incoming.TryDequeue(out string? message))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(message);
                bytes.CopyTo(buffer.Array!, buffer.Offset);

                return new WebSocketReceiveResult(bytes.Length, WebSocketMessageType.Text, true);
            }

            if (_stayOpen)
            {
                await _closeRequested.Task.WaitAsync(cancellationToken);
            }

            return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true, WebSocketCloseStatus.NormalClosure, null);
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            Sent.Enqueue(Encoding.UTF8.GetString(buffer));
            return Task.CompletedTask;
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            _closeStatus = closeStatus;
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            return CloseAsync(closeStatus, statusDescription, cancellationToken);
        }

        public override void Abort()
        {
            _state = WebSocketState.Aborted;
        }

        public override void Dispose()
        {
        }
    }
}
