using Serilog.Context;

namespace JobScraper.Web.Modules.Logging;

public class UserLogContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var userLogin = context.User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(userLogin))
        {
            await next(context);
            return;
        }

        using var pushProperty = LogContext.PushProperty("UserLogin", userLogin);
        await next(context);
    }
}
