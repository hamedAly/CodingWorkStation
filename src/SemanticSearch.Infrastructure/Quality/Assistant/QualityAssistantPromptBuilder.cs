using System.Text;
using Microsoft.Extensions.Options;
using SemanticSearch.Application.Common;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Quality.Assistant.Models;

namespace SemanticSearch.Infrastructure.Quality.Assistant;

public sealed class QualityAssistantPromptBuilder : IQualityAssistantPromptBuilder
{
    private readonly SemanticSearchOptions _options;
    private readonly DuplicateSnippetLimiter _snippetLimiter;

    public QualityAssistantPromptBuilder(
        IOptions<SemanticSearchOptions> options,
        DuplicateSnippetLimiter snippetLimiter)
    {
        _options = options.Value;
        _snippetLimiter = snippetLimiter;
    }

    public AssistantPromptModel BuildProjectPlanPrompt(ProjectPlanRequestModel request)
    {
        var systemPrompt = """
You are an expert .NET software architect and tech lead reviewing a C# and .NET codebase.
Return GitHub-flavored markdown only.
Start immediately with `1.` and provide exactly 3 numbered actions.
Every action must target duplication reduction or clone-related maintenance cost.
Use only the file paths and facts provided in the user message.
Never invent files, folders, languages, frameworks, or metrics that were not provided.
Do not output placeholders or echo the requested format.
Do not repeat the answer.
Do not recommend generic process changes like CI or code review unless the evidence explicitly points there.
Do not give vague advice like "extract common logic" by itself.
Prefer concrete .NET architectural moves such as shared application services, reusable query/command handlers, shared formatting components, MediatR pipeline behavior, common domain policies, or focused base abstractions when justified by the hotspot files.
Keep the recommendations concrete, architectural, and codebase-focused.
""";

        var userPrompt = new StringBuilder();
        userPrompt.AppendLine("Project quality snapshot:");
        userPrompt.AppendLine($"- Project key: {request.ProjectKey}");
        userPrompt.AppendLine($"- Snapshot run: {request.RunId}");
        userPrompt.AppendLine($"- Codebase language: C# on .NET");
        userPrompt.AppendLine($"- Quality grade: {request.QualityGrade}");
        userPrompt.AppendLine($"- Total lines of code: {request.TotalLinesOfCode}");
        userPrompt.AppendLine($"- Duplication percent: {request.DuplicationPercent:0.##}%");
        userPrompt.AppendLine($"- Structural clone findings: {request.StructuralFindingCount}");
        userPrompt.AppendLine($"- Semantic clone findings: {request.SemanticFindingCount}");
        userPrompt.AppendLine($"- Total duplicate findings in snapshot: {request.TotalFindingCount}");
        userPrompt.AppendLine($"- Last analyzed UTC: {request.LastAnalyzedUtc:O}");
        userPrompt.AppendLine();
        userPrompt.AppendLine("Impacted controllers from the current snapshot:");

        if (request.ImpactedControllers.Count == 0)
        {
            userPrompt.AppendLine("- No controller files were identified in the current snapshot.");
        }
        else
        {
            foreach (var controller in request.ImpactedControllers)
            {
                userPrompt.AppendLine($"- {controller}");
            }
        }

        userPrompt.AppendLine();
        userPrompt.AppendLine("Representative duplicate hotspots from the current snapshot:");

        if (request.TopHotspots.Count == 0)
        {
            userPrompt.AppendLine("- No file-level hotspots were available in the snapshot. Keep recommendations project-level and tied to the provided metrics only.");
        }
        else
        {
            foreach (var hotspot in request.TopHotspots)
            {
                userPrompt.AppendLine($"- Finding {hotspot.FindingId}: {hotspot.DuplicationType}, severity {hotspot.Severity}, similarity {hotspot.SimilarityScore:0.0000}, matching lines {hotspot.MatchingLineCount}");
                userPrompt.AppendLine($"  Left: {hotspot.LeftFilePath} ({hotspot.LeftStartLine}-{hotspot.LeftEndLine})");
                userPrompt.AppendLine($"  Right: {hotspot.RightFilePath} ({hotspot.RightStartLine}-{hotspot.RightEndLine})");
            }
        }

        userPrompt.AppendLine();
        userPrompt.AppendLine("Required output shape:");
        userPrompt.AppendLine("1. Short action title");
        userPrompt.AppendLine("   - Why: one sentence tied to the listed metrics or hotspot files");
        userPrompt.AppendLine("   - What to change: one or two concrete codebase changes referencing only listed files or project-level areas");
        userPrompt.AppendLine("   - Expected result: one observable debt reduction outcome");
        userPrompt.AppendLine("Coverage");
        userPrompt.AppendLine("   - Add a final `## Impacted Controllers` section listing every controller path from the provided list exactly once");
        userPrompt.AppendLine();
        userPrompt.AppendLine("Do not mention any `.py` files or any file path that is not listed above.");
        userPrompt.AppendLine("Stop after point 3.");
        return new AssistantPromptModel(systemPrompt, userPrompt.ToString());
    }

    public AssistantPromptModel BuildFindingFixPrompt(FindingFixRequestModel request)
    {
        if (string.IsNullOrWhiteSpace(request.LeftSnippet) || string.IsNullOrWhiteSpace(request.RightSnippet))
        {
            throw new ConflictException("The selected duplicate comparison does not have both source snippets available for refactoring guidance.");
        }

        var bounded = _snippetLimiter.Bound(request);
        var systemPrompt = """
You are a local AI tech lead refactoring duplicate C# code in a .NET codebase.
Return GitHub-flavored markdown only.
Start with a short explanation of the recommended consolidation approach.
Then return exactly one fenced ```csharp code block containing the proposed merged implementation.
Prefer a shared method or extracted service before inheritance unless a base class is clearly the best fit.
Use only the code and file paths provided in the user message.
Do not invent unrelated files or change the language from C#.
Do not include placeholders like TODO.
""";

        var userPrompt = new StringBuilder();
        userPrompt.AppendLine("Duplicate context:");
        userPrompt.AppendLine($"- Finding id: {request.FindingId}");
        userPrompt.AppendLine($"- Type: {request.DuplicationType}");
        userPrompt.AppendLine($"- Severity: {request.Severity}");
        userPrompt.AppendLine($"- Similarity score: {request.SimilarityScore:0.0000}");
        userPrompt.AppendLine($"- Left file: {request.LeftFilePath}");
        userPrompt.AppendLine($"- Left availability: {request.LeftAvailability}");
        userPrompt.AppendLine($"- Right file: {request.RightFilePath}");
        userPrompt.AppendLine($"- Right availability: {request.RightAvailability}");
        if (bounded.Truncated)
        {
            userPrompt.AppendLine("- Note: the snippets below were truncated to fit the local prompt budget.");
        }

        if (!string.Equals(request.LeftAvailability, "Available", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.RightAvailability, "Available", StringComparison.OrdinalIgnoreCase))
        {
            userPrompt.AppendLine("- Note: one or both snippets come from stored comparison data because the live source region no longer matches exactly.");
        }

        userPrompt.AppendLine();
        userPrompt.AppendLine("Left snippet:");
        userPrompt.AppendLine("```csharp");
        userPrompt.AppendLine(bounded.Left);
        userPrompt.AppendLine("```");
        userPrompt.AppendLine();
        userPrompt.AppendLine("Right snippet:");
        userPrompt.AppendLine("```csharp");
        userPrompt.AppendLine(bounded.Right);
        userPrompt.AppendLine("```");
        return new AssistantPromptModel(systemPrompt, userPrompt.ToString());
    }

    public AssistantInferenceOptionsModel BuildInferenceOptions()
        => new(
            _options.Assistant.MaxTokens,
            _options.Assistant.Temperature,
            _options.Assistant.AntiPrompts
                .Concat([
                    "<|im_end|>",
                    "<|endoftext|>",
                    "<|im_start|>user",
                    "<|im_start|>system"
                ])
                .Distinct(StringComparer.Ordinal)
                .ToArray());
}
