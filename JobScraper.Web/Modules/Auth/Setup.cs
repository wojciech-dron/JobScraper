using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.Account;
using JobScraper.Web.Modules.Persistence;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace JobScraper.Web.Modules.Auth;

public static class Setup
{
    public static WebApplicationBuilder AddAuthServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IPasswordHasher<string>, PasswordHasher<string>>();

        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<IdentityRedirectManager>();
        builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();


        var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];

        if (string.IsNullOrWhiteSpace(jwtSigningKey))
            throw new Exception("Jwt SigningKey not found in appsettings");

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies();

        builder.Services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            })
            .AddEntityFrameworkStores<JobsDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        return builder;
    }
}
