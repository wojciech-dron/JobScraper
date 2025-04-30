using Microsoft.AspNetCore.Components.QuickGrid;

namespace JobScraper.Web.Extensions;

public static class QuickGridExtensions
{
    public static async Task SortAsync<T>(this ColumnBase<T> column) =>
        await column.Grid.SortByColumnAsync(column);

}