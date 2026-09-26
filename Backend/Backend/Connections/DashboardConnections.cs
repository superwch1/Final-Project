using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Security.Claims;

namespace Backend.Connections
{
    public class DashboardConnections : BaseConnections
    {
        private readonly ConcurrentDictionary<Guid, Guid> _accountIdByConnectionId = new();
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly JsonWebTokenHandler _tokenHandler = new();

        private event Func<(Guid ConnectionId, Guid AccountId), Task>? _dashboardSignedIn;

        public DashboardConnections(IOptionsMonitor<JwtBearerOptions> jwtBearerOptions)
        {
            _tokenValidationParameters = jwtBearerOptions
                .Get(JwtBearerDefaults.AuthenticationScheme)
                .TokenValidationParameters;
        }

        /// <summary>
        /// Runs a dashboard WebSocket connection until it closes, then forgets its signed-in account
        /// </summary>
        public async Task DashboardEcho(WebSocket webSocket, CancellationToken cancellationToken)
        {
            Guid connectionId = Guid.NewGuid();
            try
            {
                await Echo(connectionId, webSocket, [], cancellationToken);
            }
            finally
            {
                _accountIdByConnectionId.TryRemove(connectionId, out _);
            }
        }

        /// <summary>
        /// Registers a handler that runs when a dashboard connection signs in
        /// </summary>
        public void SubscribeToDashboardSignedIn(Func<(Guid ConnectionId, Guid AccountId), Task> eventHandler)
            => _dashboardSignedIn += eventHandler;

        /// <summary>
        /// Sends the initial set of messages to a single dashboard connection
        /// </summary>
        public async Task SendSnapshot(Guid connectionId, IEnumerable<string> messages, CancellationToken cancellationToken)
        {
            foreach (string message in messages)
            {
                await SendMessageAsync(connectionId, message, cancellationToken);
            }
        }

        /// <summary>
        /// Sends a telemetry update to every dashboard connection signed in to the account
        /// </summary>
        public async Task NotifyTelemetryChanged(string message, Guid accountId, CancellationToken cancellationToken)
        {
            IEnumerable<Guid> connectionIds = _accountIdByConnectionId
                .ToArray()
                .Where(x => x.Value == accountId)
                .Select(x => x.Key);

            foreach (Guid connectionId in connectionIds)
            {
                await SendMessageAsync(connectionId, message, cancellationToken);
            }
        }

        /// <summary>
        /// Treats the first message from a dashboard as a JWT and signs the connection in to that account
        /// </summary>
        protected override async Task OnMessageReceived(Guid connectionId, string message)
        {
            if (_accountIdByConnectionId.ContainsKey(connectionId))
            {
                return;
            }

            Guid? accountId = await ReadAccountIdAsync(message);
            if (accountId is null)
            {
                return;
            }

            _accountIdByConnectionId[connectionId] = accountId.Value;

            if (_dashboardSignedIn != null)
            {
                await _dashboardSignedIn.Invoke((connectionId, accountId.Value));
            }
        }

        /// <summary>
        /// Validates the JWT and returns its account ID, or null if the token is invalid
        /// </summary>
        private async Task<Guid?> ReadAccountIdAsync(string token)
        {
            TokenValidationResult result = await _tokenHandler.ValidateTokenAsync(token, _tokenValidationParameters);

            if (!result.IsValid)
            {
                return null;
            }

            string? subject = result.ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? result.ClaimsIdentity.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(subject, out Guid accountId) ? accountId : null;
        }
    }
}
