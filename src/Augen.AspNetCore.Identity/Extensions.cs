using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Augen.AspNetCore.Identity
{
    public static class Extensions
    {
        /// <summary>
        /// Use the default Augen OAuth2
        /// </summary>
        /// <typeparam name="TIdentity"></typeparam>
        /// <typeparam name="TRole"></typeparam>
        /// <param name="services"></param>
        /// <param name="connectionString">NULL to use ConnectionStrings:DefaultConnection</param>
        public static void AddAugenIdentity<TIdentity, TRole>(this IServiceCollection services, string connectionString = null)
            where TIdentity : IdentityUser
            where TRole : IdentityRole
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                services.AddDbContext<IdentityDbContext>(options =>
                    options.UseSqlServer(connectionString));
            }
            else
            {
                //Default connection string
                services.AddDbContext<IdentityDbContext>();
            }

            // Register the Identity services.
            services.AddIdentity<TIdentity, TRole>()
                .AddEntityFrameworkStores<IdentityDbContext>()
                .AddDefaultTokenProviders();

            // Register the OpenIddict services, including the default Entity Framework stores.
            services.AddOpenIddict<IdentityDbContext>()
                // Register the ASP.NET Core MVC binder used by OpenIddict.
                // Note: if you don't call this method, you won't be able to
                // bind OpenIdConnectRequest or OpenIdConnectResponse parameters.
                .AddMvcBinders()

                // Enable the token endpoint (required to use the password flow).
                //.EnableTokenEndpoint("/connect/token")

                // Allow client applications to use the grant_type=password flow.
                //.AllowPasswordFlow()

                // During development, you can disable the HTTPS requirement.
                .DisableHttpsRequirement()

                // Register a new ephemeral key, that is discarded when the application
                // shuts down. Tokens signed using this key are automatically invalidated.
                // This method should only be used during development.
                .AddEphemeralSigningKey();
        }
    }
}
