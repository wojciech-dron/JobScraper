using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScraper.Web.Modules.Auth;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string AiSummary = "AiSummary";
}

public class RoleConfiguration : IEntityTypeConfiguration<IdentityRole>
{

    public void Configure(EntityTypeBuilder<IdentityRole> builder) => builder.HasData(
        new IdentityRole
        {
            Id = "6a427606-d71e-451e-9276-f363c43777f9",
            Name = AppRoles.Admin,
            ConcurrencyStamp = "019c70b8-19d0-715b-9595-2f4f60b20c02",
            NormalizedName = AppRoles.Admin.ToUpper(),
        },
        new IdentityRole
        {
            Id = "6a427606-d71e-451e-9276-f363c43777f0",
            Name = AppRoles.AiSummary,
            ConcurrencyStamp = "019c70b8-19d0-715b-9595-2f4f60b20c03",
            NormalizedName = AppRoles.AiSummary.ToUpper(),
        }
    );
}
