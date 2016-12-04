using Augen.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace HDInsight.Identity
{
    public partial class IdentityDbContext
    {
        private static void ConfigureAspNetUserOpenIddictApplication(ModelBuilder builder)
        {
            builder.Entity<AspNetUserOpenIddictApplication>().ToTable("AspNetUserOpenIddictApplications");

            var entity = builder.Entity<AspNetUserOpenIddictApplication>();

            entity.HasKey(x => new { x.UserId, x.AppId });

            entity.HasOne(x => x.User)
                .WithMany(x => x.UserOpenIddictApplications)
                .HasForeignKey(x => x.UserId);

            entity.HasOne(x => x.App)
                .WithMany(x => x.UserOpenIddictApplications)
                .HasForeignKey(x => x.AppId);
        }
    }
}
