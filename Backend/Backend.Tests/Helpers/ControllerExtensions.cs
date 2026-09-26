using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Tests.Helpers
{
    public static class ControllerExtensions
    {
        /// <summary>
        /// Sign the controller in as the account
        /// </summary>
        public static T SignedInAs<T>(this T controller, Guid? accountId) where T : ControllerBase
        {
            ClaimsIdentity identity = (accountId is null)
                ? new ClaimsIdentity()
                : new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, accountId.Value.ToString())], "Test");

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            return controller;
        }
    }
}
