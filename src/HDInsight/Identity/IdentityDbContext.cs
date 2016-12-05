using Augen.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict;

namespace HDInsight.Identity
{
    public partial class IdentityDbContext :
        OpenIddictDbContext<DefaultIdentityUser,
        DefaultIdentityRole,
        DefaultOpenIddictApplication,
        DefaultOpenIddictAuthorization,
        DefaultOpenIddictScope,
        DefaultOpenIddictToken, string>
    {
        public DbSet<AspNetUserOpenIddictApplication> UserApplications { get; set; }

        public IdentityDbContext(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            ConfigureAspNetUserOpenIddictApplication(builder);
        }
    }
}