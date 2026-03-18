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
            "Quality Analysis" => "Structural and semantic duplication review",
            "Indexing" => "Register repositories and manage refresh jobs",
            "Search" => "Semantic and exact retrieval across indexed code",
            "Explorer" => "Read indexed trees and inspect full files",
            "AI Assistant" => "Model readiness and guided remediation workflows",
            "Architecture Map" => "Roadmap for system topology visualization",
            "Dependency Graph" => "Roadmap for service and package graphing",
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
            new NavigationItem("Indexing",         Icons.Indexing,     "/indexing",  IsActive: true),
            new NavigationItem("Search",           Icons.Search,       "/search",    IsActive: true),
            new NavigationItem("Explorer",         Icons.Explorer,     "/explorer",  IsActive: true),
        ]),
        new PhaseGroup("AI Tech Lead", 2, PhaseStatus.Active,
        [
            new NavigationItem("AI Assistant", Icons.AiAssistant, "/assistant", IsActive: true),
        ]),
        new PhaseGroup("Visual Architecture", 3, PhaseStatus.ComingSoon,
        [
            new NavigationItem("Architecture Map",  Icons.Architecture, null, IsActive: false),
            new NavigationItem("Dependency Graph",  Icons.Dependency,   null, IsActive: false),
        ]),
        new PhaseGroup("TFS & Automation", 4, PhaseStatus.ComingSoon,
        [
            new NavigationItem("Automation Hub",  Icons.Automation, null, IsActive: false),
            new NavigationItem("Pipeline Status", Icons.Pipeline,   null, IsActive: false),
        ]),
        new PhaseGroup("Guardrails & Safety", 5, PhaseStatus.ComingSoon,
        [
            new NavigationItem("Code Guardrails",   Icons.Guardrails, null, IsActive: false),
            new NavigationItem("Safety Dashboard",  Icons.Safety,     null, IsActive: false),
        ]),
    ];
}
