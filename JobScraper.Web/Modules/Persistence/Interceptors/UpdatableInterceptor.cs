using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace JobScraper.Web.Modules.Persistence.Interceptors;

public interface IUpdatable
{
    DateTime? UpdatedAt { get; set; }
}

public class UpdatableInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default) => ValueTask.FromResult(SavingChanges(eventData, result));

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        var context = eventData.Context;

        if (context == null)
            return result;

        foreach (var entry in context.ChangeTracker
            .Entries<IUpdatable>()
            .Where(jo => jo.State == EntityState.Modified))
            entry.Entity.UpdatedAt = DateTime.UtcNow;

        return result;
    }
}
