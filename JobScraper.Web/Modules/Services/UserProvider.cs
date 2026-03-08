using JobScraper.Web.Modules.Persistence;
using Serilog.Context;

namespace JobScraper.Web.Modules.Services;

public interface IUserProvider
{
    public string? UserName { get; }
}

public class UserProvider : IUserProvider
{
    public string? UserName { get; set; }
}

public static class UserProviderExtensions
{
    public static IServiceCollection AddUserProvider(this IServiceCollection services)
    {
        services.AddScoped<UserProvider>();
        services.AddScoped<IUserProvider, UserProvider>(sp => sp.GetRequiredService<UserProvider>());

        return services;
    }

    public static WebApplication UseUserIdentityMiddleware(this WebApplication app)
    {
        app.UseMiddleware<UserIdentityMiddleware>();

        return app;
    }
}

/// <summary> Required for endpoints where DbContext is resolved before authentication </summary>
public class UserIdentityMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var userName = context.User.Identity?.Name;
        if (string.IsNullOrEmpty(userName))
        {
            await next(context);
            return;
        }

        using var userNameScope = LogContext.PushProperty("UserName", userName);

        var userProvider = context.RequestServices.GetRequiredService<UserProvider>();
        userProvider.UserName = userName;

        var dbContext = context.RequestServices.GetRequiredService<JobsDbContext>();
        dbContext.CurrentUserName = userName;

        await next(context);
    }

}
