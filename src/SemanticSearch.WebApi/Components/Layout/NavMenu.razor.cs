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
            "Automation Hub" => "Roadmap for pipelines and orchestration",
            "Pipeline Status" => "Roadmap for delivery signal monitoring",
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
        new PhaseGroup("TFS & Automation", 3, PhaseStatus.ComingSoon,
        [
            new NavigationItem("Automation Hub",  Icons.Automation, null, IsActive: false),
            new NavigationItem("Pipeline Status", Icons.Pipeline,   null, IsActive: false),
        ]),
        new PhaseGroup("Guardrails & Safety", 4, PhaseStatus.ComingSoon,
        [
            new NavigationItem("Code Guardrails",   Icons.Guardrails, null, IsActive: false),
            new NavigationItem("Safety Dashboard",  Icons.Safety,     null, IsActive: false),
        ]),
    ];
}
