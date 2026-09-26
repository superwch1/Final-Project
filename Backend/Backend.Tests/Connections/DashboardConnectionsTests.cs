using Backend.Connections;
using Backend.Models;
using Backend.Services;
using Backend.Tests.Helpers;
using Microsoft.Extensions.Options;

namespace Backend.Tests.Connections
{
    public class DashboardConnectionsTests
    {
        private readonly DashboardConnections _dashboardConnections = new(TestOptions.JwtBearer());
        private readonly JwtTokenService _jwtTokenService = new(Options.Create(TestOptions.Jwt));
        private readonly List<(Guid ConnectionId, Guid AccountId)> _signedIn = [];
        private readonly Account _account = TestData.Account();

        public DashboardConnectionsTests()
        {
            _dashboardConnections.SubscribeToDashboardSignedIn(eventArgs =>
            {
                _signedIn.Add(eventArgs);
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task DashboardEcho_ValidToken_SignsInAccount()
        {
            await _dashboardConnections.DashboardEcho(new FakeWebSocket([Token(_account)]), CancellationToken.None);

            Assert.Equal(_account.Id, Assert.Single(_signedIn).AccountId);
        }

        [Fact]
        public async Task DashboardEcho_InvalidToken_DoesNotSignIn()
        {
            await _dashboardConnections.DashboardEcho(new FakeWebSocket(["not a token"]), CancellationToken.None);

            Assert.Empty(_signedIn);
        }

        [Fact]
        public async Task DashboardEcho_TokenSignedWithAnotherKey_DoesNotSignIn()
        {
            JwtTokenService otherService = new(Options.Create(new JwtOptions
            {
                Key = "another-signing-key-that-is-long-enough-for-hmac",
                Issuer = TestOptions.Jwt.Issuer,
                Audience = TestOptions.Jwt.Audience,
                Lifetime = TestOptions.Jwt.Lifetime
            }));

            await _dashboardConnections.DashboardEcho(new FakeWebSocket([otherService.CreateToken(_account)]), CancellationToken.None);

            Assert.Empty(_signedIn);
        }

        [Fact]
        public async Task DashboardEcho_AlreadySignedIn_IgnoresSecondToken()
        {
            await _dashboardConnections.DashboardEcho(new FakeWebSocket([Token(_account), Token(TestData.Account())]), CancellationToken.None);

            Assert.Single(_signedIn);
        }

        [Fact]
        public async Task SendSnapshot_SignedInDashboard_SendsMessagesInOrder()
        {
            DashboardConnections dashboardConnections = new(TestOptions.JwtBearer());
            dashboardConnections.SubscribeToDashboardSignedIn(eventArgs =>
                dashboardConnections.SendSnapshot(eventArgs.ConnectionId, ["first", "second"], CancellationToken.None));
            FakeWebSocket webSocket = new([Token(_account)]);

            await dashboardConnections.DashboardEcho(webSocket, CancellationToken.None);

            Assert.Equal(["first", "second"], webSocket.Sent);
        }

        [Fact]
        public async Task NotifyTelemetryChanged_SignedInAccount_SendsMessage()
        {
            FakeWebSocket webSocket = new([Token(_account)], stayOpen: true);
            Task echo = _dashboardConnections.DashboardEcho(webSocket, CancellationToken.None);
            await Wait.UntilAsync(() => _signedIn.Count == 1);

            await _dashboardConnections.NotifyTelemetryChanged("reading", _account.Id, CancellationToken.None);
            webSocket.RequestClose();
            await echo;

            Assert.Contains("reading", webSocket.Sent);
        }

        [Fact]
        public async Task NotifyTelemetryChanged_AnotherAccount_SendsNothing()
        {
            FakeWebSocket webSocket = new([Token(_account)], stayOpen: true);
            Task echo = _dashboardConnections.DashboardEcho(webSocket, CancellationToken.None);
            await Wait.UntilAsync(() => _signedIn.Count == 1);

            await _dashboardConnections.NotifyTelemetryChanged("reading", Guid.NewGuid(), CancellationToken.None);
            webSocket.RequestClose();
            await echo;

            Assert.Empty(webSocket.Sent);
        }

        /// <summary>
        /// Creates an access token for the account
        /// </summary>
        private string Token(Account account)
        {
            return _jwtTokenService.CreateToken(account);
        }
    }
}
