using Microsoft.AspNetCore.Components;

namespace JobScraper.Web.Components.Components.Select;

public partial class MySelect<TItem> : ComponentBase
{
    /// <summary>
    /// Represents the selected option.
    /// </summary>
    [Parameter]
    public TItem? SelectedOption { get; set; }

    /// <summary>
    /// Event callback that is invoked when the selected option change.
    /// </summary>
    [Parameter]
    public EventCallback<TItem?> SelectedOptionChanged { get; set; }

    /// <summary>
    /// Represents the selected options in the multiselect dropdown.
    /// </summary>
    [Parameter]
    public TItem[] SelectedOptions { get; set; } = [];

    /// <summary>
    /// Event callback that is invoked when the selected options change.
    /// </summary>
    [Parameter]
    public EventCallback<TItem[]> SelectedOptionsChanged { get; set; }

    /// <summary>
    /// Represents the available options in the multiselect dropdown.
    /// </summary>
    [Parameter]
    public ICollection<TItem> Options { get; set; } = [];

    /// <summary>
    /// Function to convert an item to a string for display in the dropdown.
    /// </summary>
    [Parameter]
    public Func<TItem, string> StringSelector { get; set; } = item => item?.ToString() ?? string.Empty;

    /// <summary>
    /// The default text to display when no options are selected.
    /// </summary>
    [Parameter]
    public string DefaultText { get; set; } = "None";

    /// <summary>
    /// Optional custom renderer for the selected options.
    /// If not set, items will be rendered using <see cref="StringSelector"/> and concatenated with a comma.
    /// </summary>
    [Parameter]
    public RenderFragment<ICollection<TItem>>? SelectedOptionsRenderer { get; set; }

    /// <summary>
    /// Whether the dropdown should be filterable.
    /// </summary>
    [Parameter]
    public bool CanFilter { get; set; }

    /// <summary>
    /// A function that determines whether a given item matches the filter string.
    /// Default implementation checks if the item's string representation contains the filter string, case-insensitive.
    /// </summary>
    /// <returns>Returns true if the item's string representation contains the filter string, false otherwise.</returns>
    [Parameter]
    public Func<TItem, string, bool>? FilterPredicate { get; set; }

    private bool DefaultFilterPredicate(TItem item, string filterString)
    {
        return StringSelector(item).Contains(filterString, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Whether the virtualize component should be used to render the options.
    /// </summary>
    [Parameter]
    public bool Virtualize { get; set; }

    /// <summary>
    /// Whether the multiselect should allow multiple selections.
    /// </summary>
    [Parameter]
    public bool MultiSelect { get; set; } = false;

    /// <summary>
    /// The id the input element should have.
    /// </summary>
    [Parameter]
    public string? Id { get; set; }

    /// <summary>
    /// Additional CSS classes to apply.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Additional CSS styles to apply.
    /// </summary>
    [Parameter]
    public string? Style { get; set; }

    /// <summary>
    /// Whether the multiselect is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// If true, the SimpleMultiselect component will be rendered properly without needing Bootstrap.
    /// </summary>
    [CascadingParameter(Name = "Standalone")]
    public bool Standalone { get; set; }
    
    private async Task ToggleOption(TItem option)
    {
        var newSelectedOptions = new List<TItem>(SelectedOptions);

        var adding = !newSelectedOptions.Remove(option);
        if (adding)
        {
            if (!MultiSelect)
            {
                newSelectedOptions.Clear();
            }

            newSelectedOptions.Add(option);
        }

        SelectedOptions = newSelectedOptions.ToArray();
        await SelectedOptionsChanged.InvokeAsync(SelectedOptions);

        SelectedOption = SelectedOptions.FirstOrDefault();
        await SelectedOptionChanged.InvokeAsync(SelectedOption);

        if (!MultiSelect)
        {
            await CloseDropdown();
        }
    }

    private bool IsOptionSelected(TItem option)
    {
        return SelectedOptions.Contains(option);
    }

    private List<TItem>? _filteredOptionsCache;
    private ICollection<TItem>? _prevOptions;
    private Func<TItem, string, bool>? _prevFilterPredicate;
    private string? _prevFilterText;
    private bool _prevCanFilter;
    
    private ICollection<TItem> FilteredOptions()
    {
        if(_prevCanFilter == CanFilter && _prevFilterPredicate == FilterPredicate && _prevFilterText == _filterText && _prevOptions == Options)
        {
            return _filteredOptionsCache ?? Options;
        }
        
        _prevOptions = Options;
        _prevCanFilter = CanFilter;
        _prevFilterPredicate = FilterPredicate;
        _prevFilterText = _filterText;
        
        _filteredOptionsCache = [];
        if(!CanFilter || string.IsNullOrWhiteSpace(_filterText))
        {
            _filteredOptionsCache.AddRange(Options);
        }
        else
        {
            var predicate = FilterPredicate ?? DefaultFilterPredicate;
            _filteredOptionsCache.AddRange(Options.Where(option => predicate(option, _filterText)));
        }
        
        return _filteredOptionsCache;
    }
}