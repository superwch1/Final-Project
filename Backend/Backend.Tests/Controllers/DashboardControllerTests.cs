using Backend.Connections;
using Backend.Controllers;
using Backend.Tests.Helpers;
using Moq;

namespace Backend.Tests.Controllers
{
    public class DashboardControllerTests
    {
        [Fact]
        public async Task WebSocket_PlainHttpRequest_Returns400()
        {
            DashboardController controller = new DashboardController(new Mock<IConnectionMediator>().Object).SignedInAs(null);

            await controller.WebSocket(CancellationToken.None);

            Assert.Equal(400, controller.HttpContext.Response.StatusCode);
        }

        [Fact]
        public async Task WebSocket_PlainHttpRequest_DoesNotStartEcho()
        {
            Mock<IConnectionMediator> connectionMediator = new();
            DashboardController controller = new DashboardController(connectionMediator.Object).SignedInAs(null);

            await controller.WebSocket(CancellationToken.None);

            connectionMediator.VerifyNoOtherCalls();
        }
    }
}
