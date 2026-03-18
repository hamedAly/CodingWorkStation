using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using SemanticSearch.WebApi.Models.Navigation;

namespace SemanticSearch.WebApi.Components.Layout;

public partial class TopBar : IDisposable
{
    [Parameter] public bool Collapsed { get; set; }
    [Parameter] public EventCallback OnOpenMobileSidebar { get; set; }

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private IReadOnlyList<BreadcrumbSegment> _breadcrumbs = Array.Empty<BreadcrumbSegment>();

    protected override void OnInitialized()
    {
        _breadcrumbs = BreadcrumbMap.GetBreadcrumbs(new Uri(NavigationManager.Uri).AbsolutePath);
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        _breadcrumbs = BreadcrumbMap.GetBreadcrumbs(new Uri(e.Location).AbsolutePath);
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
