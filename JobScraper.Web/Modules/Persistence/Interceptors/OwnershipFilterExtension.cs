using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.Web.Modules.Persistence.Interceptors;

public static class OwnershipFilterExtension
{
    public static void ApplyOwnershipFilter(
        this ModelBuilder modelBuilder,
        Expression<Func<string?>> userIdExpression)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IOwnable).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");

            // 1. Get the 'Owner' property from the entity: e.Owner
            var property = Expression.Property(parameter, nameof(IOwnable.Owner));

            // 2. The value to compare against comes from the expression body
            var filterValue = userIdExpression.Body;

            // 3. Combine: e.Owner == userIdExpression
            var comparison = Expression.Equal(property, filterValue);
            var lambda = Expression.Lambda(comparison, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}
