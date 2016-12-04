using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using OpenIddict;
using System.Collections.Generic;

namespace Augen.AspNetCore.Identity
{
    public class DefaultIdentityRole : IdentityRole
    {
    }

    public class DefaultIdentityUser : IdentityUser
    {
        public List<AspNetUserOpenIddictApplication> UserOpenIddictApplications { get; set; }
    }

    public class AspNetUserOpenIddictApplication
    {
        public string UserId { get; set; }
        public DefaultIdentityUser User { get; set; }

        public string AppId { get; set; }
        public DefaultOpenIddictApplication App { get; set; }
    }

    public class DefaultOpenIddictScope : OpenIddictScope { }

    public class DefaultOpenIddictToken : OpenIddictToken { }

    public class DefaultOpenIddictApplication : OpenIddictApplication<string, DefaultOpenIddictToken>
    {
        public List<AspNetUserOpenIddictApplication> UserOpenIddictApplications { get; set; }
    }

    public class DefaultOpenIddictAuthorization : OpenIddictAuthorization<string, DefaultOpenIddictToken> { }
}
