using AspNet.Security.OpenIdConnect.Extensions;
using AspNet.Security.OpenIdConnect.Primitives;
using AspNet.Security.OpenIdConnect.Server;
using Augen.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenIddict;
using System.Security.Claims;

namespace HDInsight.Controllers
{
    public abstract class OpenIddictControllerBase : Controller
    {
        protected OpenIddictApplicationManager<DefaultOpenIddictApplication> OpenIdAppManager { get; private set; }

        public OpenIddictControllerBase(OpenIddictApplicationManager<DefaultOpenIddictApplication> openAppManager)
        {
            OpenIdAppManager = openAppManager;
        }

        protected virtual AuthenticationTicket CreateTicket(OpenIdConnectRequest request, DefaultOpenIddictApplication application)
        {
            // Create a new ClaimsIdentity containing the claims that
            // will be used to create an id_token, a token or a code.
            var identity = new ClaimsIdentity(OpenIdConnectServerDefaults.AuthenticationScheme);

            // Use the client_id as the name identifier.
            identity.AddClaim(ClaimTypes.NameIdentifier, application.ClientId,
                OpenIdConnectConstants.Destinations.AccessToken,
                OpenIdConnectConstants.Destinations.IdentityToken);

            identity.AddClaim(ClaimTypes.Name, application.DisplayName,
                OpenIdConnectConstants.Destinations.AccessToken,
                OpenIdConnectConstants.Destinations.IdentityToken);

            // Create a new authentication ticket holding the user identity.
            return new AuthenticationTicket(
                new ClaimsPrincipal(identity),
                new AuthenticationProperties(),
                OpenIdConnectServerDefaults.AuthenticationScheme);
        }
    }
}
