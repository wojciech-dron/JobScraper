using Microsoft.AspNetCore.DataProtection;

namespace JobScraper.Security;

public static class Setup
{
    public static WebApplicationBuilder ConfigureSecurity(this WebApplicationBuilder builder)
    {
        builder.Services.AddDataProtection()
            .SetApplicationName(builder.Environment.ApplicationName)
            .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["SecuritySettings:PersistKeysDirectory"]!));

        return builder;
    }
}
