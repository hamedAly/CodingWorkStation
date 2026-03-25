using Microsoft.AspNetCore.Components;
using SemanticSearch.WebApi.Models.Navigation;

namespace SemanticSearch.WebApi.Components.Layout;

public partial class NavMenu
{
    [Parameter] public bool Collapsed { get; set; }
    [Parameter] public EventCallback OnToggleCollapse { get; set; }

    private static string GetItemDescription(string label)
        => label switch
        {
            "Dashboard" => "Portfolio health and live workspace status",
            "Quality Analysis" => "Duplication review, dependency graph, heatmap, and data model",
            "Workspace" => "Register repositories, search code, and explore files",
            "My Work" => "Kanban board of your TFS work items by state",
            "PR Radar" => "Active pull requests you created or are reviewing",
            "Contributions" => "Commit and work-item activity heatmap",
            "Integration Settings" => "TFS and Slack credentials, standup and prayer automation",
            "Background Jobs" => "Hangfire dashboard for job monitoring and management",
            "Code Guardrails" => "Roadmap for policy-driven code checks",
            "Safety Dashboard" => "Roadmap for release and quality guardrails",
            _ => "Workspace module"
        };

    private static readonly IReadOnlyList<PhaseGroup> PhaseGroups =
    [
        new PhaseGroup("Code Quality", 1, PhaseStatus.Active,
        [
            new NavigationItem("Dashboard",        Icons.Dashboard,    "/",          IsActive: true),
            new NavigationItem("Quality Analysis", Icons.Quality,      "/quality",   IsActive: true),
        ]),
        new PhaseGroup("Developer Workspace", 2, PhaseStatus.Active,
        [
            new NavigationItem("Workspace", Icons.Indexing, "/workspace", IsActive: true),
        ]),
        new PhaseGroup("TFS & Automation", 3, PhaseStatus.Active,
        [
            new NavigationItem("My Work",               Icons.Quality,      "/my-work",              IsActive: true),
            new NavigationItem("PR Radar",              Icons.Dependency,   "/pull-requests",        IsActive: true),
            new NavigationItem("Contributions",         Icons.Dashboard,    "/contributions",        IsActive: true),
            new NavigationItem("Integration Settings",  Icons.Automation,   "/integration",          IsActive: true),
            new NavigationItem("Background Jobs",       Icons.Pipeline,     "/hangfire",             IsActive: true),
        ]),
        new PhaseGroup("Guardrails & Safety", 4, PhaseStatus.ComingSoon,
        [
            new NavigationItem("Code Guardrails",   Icons.Guardrails, null, IsActive: false),
            new NavigationItem("Safety Dashboard",  Icons.Safety,     null, IsActive: false),
        ]),
    ];
}
