using Augen.AspNetCore.Identity;
using CryptoHelper;
using HDInsight.Identity;
using HDInsight.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Linq;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace HDInsight.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<DefaultIdentityUser> _userManager;
        private readonly SignInManager<DefaultIdentityUser> _signInManager;
        private readonly RoleManager<DefaultIdentityRole> _roleManager;
        private readonly IdentityDbContext _identityContext;


        public AccountController(
            UserManager<DefaultIdentityUser> userManager,
            SignInManager<DefaultIdentityUser> signInManager,
            RoleManager<DefaultIdentityRole> roleManager,
            IdentityDbContext identityContext)
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
                //New app
                var newApp = new DefaultOpenIddictApplication
                {
                    ClientId = Guid.NewGuid().ToString(),
                    ClientSecret = Crypto.HashPassword(Guid.NewGuid().ToString()),
                    DisplayName = model.NewAppName,
                    Type = OpenIddictConstants.ClientTypes.Confidential
                };

                _identityContext.Applications.Add(newApp);
                _identityContext.SaveChanges();

                //New mapping
                _identityContext.UserApplications.Add(new AspNetUserOpenIddictApplication
                {
                    AppId = newApp.Id,
                    UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                });
                _identityContext.SaveChanges();

                return RedirectToAction("Manage");
            }

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
                        ClientSecret = app.App.ClientSecret
                    });
                }
            }

            return result;
        }
    }
}
