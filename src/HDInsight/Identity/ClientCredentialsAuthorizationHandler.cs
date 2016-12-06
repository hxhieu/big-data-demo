using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HDInsight.Identity.Requirements
{
    public class ClientCredentialsRequirement : IAuthorizationRequirement { }

    public class ClientCredentialsAuthorizationHandler : AuthorizationHandler<ClientCredentialsRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ClientCredentialsRequirement requirement)
        {
            var actionContext = context.Resource as ActionContext;
            if (actionContext != null && actionContext.HttpContext != null && actionContext.HttpContext.Request != null)
            {
                var request = actionContext.HttpContext.Request;
                var headers = actionContext.HttpContext.Request.Headers;
                var clientId = headers["client_id"];
                var clientSecret = headers["client_secret"];
                if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret))
                {
                    //Get the token via API
                    using (var client = new HttpClient())
                    {
                        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, request.Scheme + "://" + request.Host.ToString() + "/Account/GetAuthToken");
                        tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                        {
                            ["grant_type"] = "client_credentials",
                            ["client_id"] = clientId,
                            ["client_secret"] = clientSecret
                        });

                        try
                        {
                            var response = client.SendAsync(tokenRequest, HttpCompletionOption.ResponseContentRead).Result;
                            response.EnsureSuccessStatusCode();

                            var payload = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                            if (payload["error"] == null)
                            {
                                //Add the access token to user claim for later use
                                context.User.AddIdentity(new ClaimsIdentity(new Claim[] {
                                new Claim(CustomClaimTypes.AccessToken, (string)payload["access_token"])
                            }));
                                context.Succeed(requirement);
                                return Task.CompletedTask;
                            }
                        }
                        catch (Exception ex)
                        {
                            context.Fail();
                            return Task.CompletedTask;
                        }
                    }
                }
            }

            context.Fail();
            return Task.CompletedTask;
        }
    }
}
