using Augen.AspNetCore.Identity.Roles;
using Augen.AspNetCore.Identity.Users;
using Microsoft.EntityFrameworkCore;
using OpenIddict;

namespace HDInsight.Identity
{
    public class IdentityDbContext : OpenIddictDbContext<DefaultIdentityUser, DefaultIdentityRole>
    {
        public IdentityDbContext(DbContextOptions options)
        : base(options) {
        }
    }
}