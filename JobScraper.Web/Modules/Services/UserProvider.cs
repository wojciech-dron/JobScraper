namespace JobScraper.Web.Modules.Services;

public interface IUserProvider
{
    public string? UserName { get; }
}

public class UserProvider(IHttpContextAccessor httpContextAccessor) : IUserProvider
{
    public string? UserName { get; set; } = httpContextAccessor.HttpContext?.User.Identity?.Name;
}

public static class UserProviderExtensions
{
    public static IServiceCollection AddUserProvider(this IServiceCollection services)
    {
        services.AddScoped<UserProvider>();
        services.AddScoped<IUserProvider, UserProvider>(sp => sp.GetRequiredService<UserProvider>());

        return services;
    }
}
