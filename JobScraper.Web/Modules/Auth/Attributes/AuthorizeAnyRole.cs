using Microsoft.AspNetCore.Authorization;

namespace JobScraper.Web.Modules.Auth.Attributes;

public class AuthorizeAnyRole : AuthorizeAttribute
{
    public AuthorizeAnyRole(params string[] roles) => Roles = string.Join(", ", roles);
}
