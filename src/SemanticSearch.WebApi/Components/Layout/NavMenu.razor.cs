using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using SemanticSearch.WebApi.Models.Navigation;
using SemanticSearch.WebApi.Services;

namespace SemanticSearch.WebApi.Components.Layout;

public partial class NavMenu : IDisposable
{
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private WorkspaceApiClient ApiClient { get; set; } = default!;

    private string? _openGroupName;
    private CancellationTokenSource? _refreshLoopCts;
    private int _dueCardCount;

    protected override async Task OnInitializedAsync()
    {
        Nav.LocationChanged += HandleLocationChanged;
        _refreshLoopCts = new CancellationTokenSource();
        await LoadDueCardCountAsync();
        _ = RefreshDueCountLoopAsync(_refreshLoopCts.Token);
    }

    public void Dispose()
    {
        Nav.LocationChanged -= HandleLocationChanged;
        _refreshLoopCts?.Cancel();
        _refreshLoopCts?.Dispose();
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        _openGroupName = null;
        if (e.Location.Contains("/study/review", StringComparison.OrdinalIgnoreCase))
            _ = InvokeAsync(LoadDueCardCountAsync);
        InvokeAsync(StateHasChanged);
    }

    private async Task LoadDueCardCountAsync()
    {
        var dueCards = await ApiClient.GetDueCardsAsync();
        _dueCardCount = dueCards?.TotalCount ?? 0;
        await InvokeAsync(StateHasChanged);
    }

    private async Task RefreshDueCountLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await LoadDueCardCountAsync();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void ToggleGroup(string groupName) =>
        _openGroupName = _openGroupName == groupName ? null : groupName;

    private void ClosePanel() => _openGroupName = null;

    private static MarkupString GetGroupIcon(string groupName) => groupName switch
    {
        "Code Quality"        => Icons.Quality,
        "Developer Workspace" => Icons.Indexing,
        "TFS & Automation"   => Icons.Automation,
        "Guardrails & Safety" => Icons.Guardrails,
        "Study Hub"          => Icons.Study,
        _                    => Icons.Dashboard
    };

    private static string GetItemDescription(string label)
        => label switch
        {
            "Quality Analysis"     => "Duplication review, dependency graph, heatmap, and data model",
            "Workspace"            => "Register repositories, search code, and explore files",
            "My Work"              => "Kanban board of your TFS work items by state",
            "PR Radar"             => "Active pull requests you created or are reviewing",
            "Contributions"        => "Commit and work-item activity heatmap",
            "Integration Settings" => "TFS and Slack credentials, standup and prayer automation",
            "Background Jobs"      => "Hangfire dashboard for job monitoring and management",
            "Code Guardrails"      => "Roadmap for policy-driven code checks",
            "Safety Dashboard"     => "Roadmap for release and quality guardrails",
            "Study Library"        => "Upload books, organize chapters, and read PDFs",
            "Review Cards"         => "Spaced repetition review sessions and decks",
            "Study Dashboard"      => "Pomodoro sessions, streaks, and study analytics",
            _                      => "Workspace module"
        };

    private static readonly IReadOnlyList<PhaseGroup> PhaseGroups =
    [
        new PhaseGroup("Code Quality", 1, PhaseStatus.Active,
        [
            new NavigationItem("Quality Analysis", Icons.Quality, "/quality", IsActive: true),
        ]),
        new PhaseGroup("Developer Workspace", 2, PhaseStatus.Active,
        [
            new NavigationItem("Workspace", Icons.Indexing, "/workspace", IsActive: true),
        ]),
        new PhaseGroup("TFS & Automation", 3, PhaseStatus.Active,
        [
            new NavigationItem("My Work",               Icons.Quality,    "/my-work",       IsActive: true),
            new NavigationItem("PR Radar",              Icons.Dependency, "/pull-requests", IsActive: true),
            new NavigationItem("Contributions",         Icons.Dashboard,  "/contributions", IsActive: true),
            new NavigationItem("Integration Settings",  Icons.Automation, "/integration",   IsActive: true),
            new NavigationItem("Background Jobs",       Icons.Pipeline,   "/hangfire",      IsActive: true),
        ]),
        new PhaseGroup("Guardrails & Safety", 4, PhaseStatus.ComingSoon,
        [
            new NavigationItem("Code Guardrails",  Icons.Guardrails, null, IsActive: false),
            new NavigationItem("Safety Dashboard", Icons.Safety,     null, IsActive: false),
        ]),
        new PhaseGroup("Study Hub", 5, PhaseStatus.Active,
        [
            new NavigationItem("Study Library", Icons.Study, "/study", IsActive: true),
            new NavigationItem("Review Cards", Icons.Cards, "/study/review", IsActive: true),
            new NavigationItem("Study Dashboard", Icons.Dashboard, "/study/dashboard", IsActive: true),
        ]),
    ];
}
