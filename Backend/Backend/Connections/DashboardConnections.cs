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

        public void SubscribeToDashboardSignedIn(Func<(Guid ConnectionId, Guid AccountId), Task> eventHandler)
            => _dashboardSignedIn += eventHandler;

        /// <summary>
        /// Send the account's devices to the dashboard.
        /// </summary>
        public async Task SendSnapshot(Guid connectionId, IEnumerable<string> messages, CancellationToken cancellationToken)
        {
            foreach (string message in messages)
            {
                await SendMessageAsync(connectionId, message, cancellationToken);
            }
        }

        /// <summary>
        /// Send telemetry only to the dashboards of the account that owns the device.
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
        /// Validate the token and read the account.
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
