using Augen.AspNetCore.Identity.Users;
using Microsoft.EntityFrameworkCore;
using OpenIddict;

namespace Augen.AspNetCore.Identity
{
    public class IdentityDbContext : OpenIddictDbContext<DefaultIdentityUser>
    {
        public IdentityDbContext(DbContextOptions options)
        : base(options) {
        }
    }
}