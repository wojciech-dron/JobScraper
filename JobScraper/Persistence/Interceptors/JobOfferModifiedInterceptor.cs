using JobScraper.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace JobScraper.Persistence.Interceptors;

public class JobOfferModifiedInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        var context = eventData.Context;

        if (context == null)
            return result;

        foreach (var entry in context.ChangeTracker
            .Entries<JobOffer>()
            .Where(jo => jo.State == EntityState.Modified))
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return result;
    }
}