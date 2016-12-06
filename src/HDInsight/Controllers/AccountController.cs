using AspNet.Security.OpenIdConnect.Primitives;
using Augen.AspNetCore.Identity;
using CryptoHelper;
using HDInsight.Identity;
using HDInsight.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace HDInsight.Controllers
{
    [Authorize]
    public class AccountController : OpenIddictControllerBase
    {
        private readonly UserManager<DefaultIdentityUser> _userManager;
        private readonly SignInManager<DefaultIdentityUser> _signInManager;
        private readonly RoleManager<DefaultIdentityRole> _roleManager;
        private readonly IdentityDbContext _identityContext;

        public AccountController(
            UserManager<DefaultIdentityUser> userManager,
            SignInManager<DefaultIdentityUser> signInManager,
            RoleManager<DefaultIdentityRole> roleManager,
            IdentityDbContext identityContext,
            OpenIddictApplicationManager<DefaultOpenIddictApplication> openIdAppManager) : base(openIdAppManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _identityContext = identityContext;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Create()
        {
            return View();
        }

        public async Task<IActionResult> Manage()
        {
            var model = new ManageAccountModel
            {
                OpenIdApps = await GetUserOpenIdApps()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateApp(ManageAccountModel model)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(model.Secret)) model.Secret = Guid.NewGuid().ToString();

                //New App
                var newAppId = await OpenIdAppManager.CreateAsync(new DefaultOpenIddictApplication
                {
                    ClientId = Guid.NewGuid().ToString(),
                    ClientSecret = Crypto.HashPassword(model.Secret),
                    DisplayName = model.Name,

                    // Note: use "public" for JS/mobile/desktop applications
                    // and "confidential" for server-side applications.
                    Type = OpenIddictConstants.ClientTypes.Confidential
                });

                //New UserApp
                _identityContext.UserApplications.Add(new AspNetUserOpenIddictApplication
                {
                    AppId = newAppId,
                    UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    SecretClearText = model.Secret
                });

                _identityContext.SaveChanges();

                return RedirectToAction("Manage");
            }

            //Model error
            model.OpenIdApps = await GetUserOpenIdApps();
            return View("Manage", model);
        }

        public async Task<IActionResult> DeleteApp(string id)
        {
            _identityContext.Entry(_identityContext.UserApplications.Single(x =>
                x.AppId == id && x.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier))).State = EntityState.Deleted;

            _identityContext.Entry(_identityContext.Applications.Single(x =>
                x.Id == id)).State = EntityState.Deleted;

            await _identityContext.SaveChangesAsync();

            return RedirectToAction("Manage");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAccountModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new DefaultIdentityUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    return View();
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, true, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View("Index", model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View("Index", model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index");
        }

        [HttpPost("~/Account/GetAuthToken")]
        [AllowAnonymous]
        [Produces("application/json")]
        public async Task<IActionResult> Exchange(OpenIdConnectRequest request)
        {
            if (request.IsClientCredentialsGrantType())
            {
                // Note: the client credentials are automatically validated by OpenIddict:
                // if client_id or client_secret are invalid, this action won't be invoked.

                var application = await OpenIdAppManager.FindByClientIdAsync(request.ClientId);
                if (application == null)
                {
                    return BadRequest(new OpenIdConnectResponse
                    {
                        Error = OpenIdConnectConstants.Errors.InvalidClient,
                        ErrorDescription = "The client application was not found in the database."
                    });
                }

                // Create a new authentication ticket.
                var ticket = CreateTicket(request, application);

                return SignIn(ticket.Principal, ticket.Properties, ticket.AuthenticationScheme);
            }

            return BadRequest(new OpenIdConnectResponse
            {
                Error = OpenIdConnectConstants.Errors.UnsupportedGrantType,
                ErrorDescription = "The specified grant type is not supported."
            });
        }

        private async Task<List<ManageAccountOpenIdAppModel>> GetUserOpenIdApps()
        {
            var result = new List<ManageAccountOpenIdAppModel>();

            //EF Core lazy loading is not yet possible so we can't use _userManager but query the context directly with Include
            var identityUser = await _identityContext.Users
                .Include(x => x.UserOpenIddictApplications).ThenInclude(x => x.App)
                .Include(x => x.UserOpenIddictApplications).ThenInclude(x => x.User)
                .SingleAsync(x => x.UserName == User.Identity.Name);

            if (identityUser != null && identityUser.UserOpenIddictApplications != null)
            {
                foreach (var app in identityUser.UserOpenIddictApplications.OrderBy(x => x.App.DisplayName))
                {
                    result.Add(new ManageAccountOpenIdAppModel
                    {
                        Id = app.AppId,
                        Name = app.App.DisplayName,
                        ClientId = app.App.ClientId,
                        ClientSecret = app.SecretClearText
                    });
                }
            }

            return result;
        }
    }
}
