using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace JobScraper.Modules.Auth;

public static class Setup
{
    public static WebApplicationBuilder AddAuthServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IPasswordHasher<string>, PasswordHasher<string>>();


        var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];

        if (string.IsNullOrWhiteSpace(jwtSigningKey))
            throw new Exception("Jwt SigningKey not found in appsettings");

        builder.Services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer",
                options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            context.Token = context.Request.Cookies["access_token"];
                            return Task.CompletedTask;
                        },
                    };
                });

        builder.Services.AddAuthorization();

        return builder;
    }
}
