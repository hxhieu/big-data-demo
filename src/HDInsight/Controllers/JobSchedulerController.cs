using Augen.AspNetCore.Identity;
using HDInsight.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using OpenIddict;
using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace HDInsight.Controllers
{
    [Route("api/scheduler")]
    public class JobSchedulerController : OpenIddictControllerBase
    {
        public JobSchedulerController(OpenIddictApplicationManager<DefaultOpenIddictApplication> openIdAppManager) : base(openIdAppManager) { }

        [Route("ping")]
        [HttpPost]
        [Authorize(Policy = "client_credentials")]
        public IActionResult Ping()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    //From claim which has been addedd via the custom policy handler
                    User.FindFirst(CustomClaimTypes.AccessToken).Value
                );

                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, Request.Scheme + "://" + Request.Host.ToString() + "/api/InsightPhoto/GetTop10PhotoBigQuery");
                    var response = client.SendAsync(request, HttpCompletionOption.ResponseContentRead).Result;
                    response.EnsureSuccessStatusCode();

                    return Ok(JObject.Parse(response.Content.ReadAsStringAsync().Result));
                }
                catch (Exception ex)
                {
                    return BadRequest(ex);
                }
            }
        }
    }
}
