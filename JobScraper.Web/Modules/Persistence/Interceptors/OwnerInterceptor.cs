using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace JobScraper.Web.Modules.Persistence.Interceptors;

public interface IOwnable
{
    /// <remarks> Owner interceptor sets this on save changes</remarks>
    public string? Owner { get; set; }
}

public class OwnerInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(SavingChanges(eventData, result));

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not JobsDbContext context)
            return result;

        foreach (var entry in context.ChangeTracker
            .Entries<IOwnable>()
            .Where(jo => jo.State == EntityState.Added))
        {
            if (context.CurrentUserName is null)
                throw new InvalidOperationException("Cannot create ownable entity with null owner");

            entry.Entity.Owner = context.CurrentUserName;
        }

        return result;
    }
}
