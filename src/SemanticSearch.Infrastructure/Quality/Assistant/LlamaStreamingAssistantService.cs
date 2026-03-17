using Microsoft.Extensions.Logging;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Quality.Assistant.Models;
using System.Text;

namespace SemanticSearch.Infrastructure.Quality.Assistant;

public sealed class LlamaStreamingAssistantService : IQualityAssistantService
{
    private readonly IAiAssistantModelProvider _modelProvider;
    private readonly IQualityAssistantPromptBuilder _promptBuilder;
    private readonly ILogger<LlamaStreamingAssistantService> _logger;

    public LlamaStreamingAssistantService(
        IAiAssistantModelProvider modelProvider,
        IQualityAssistantPromptBuilder promptBuilder,
        ILogger<LlamaStreamingAssistantService> logger)
    {
        _modelProvider = modelProvider;
        _promptBuilder = promptBuilder;
        _logger = logger;
    }

    public IAsyncEnumerable<AiStreamEventModel> StreamProjectPlanAsync(
        ProjectPlanRequestModel request,
        CancellationToken cancellationToken = default)
        => StreamAsync(
            $"project-plan-{request.ProjectKey}",
            _promptBuilder.BuildProjectPlanPrompt(request),
            _promptBuilder.BuildInferenceOptions().MaxTokens,
            BuildProjectPlanCoverageAppendix(request),
            cancellationToken);

    public IAsyncEnumerable<AiStreamEventModel> StreamFindingFixAsync(
        FindingFixRequestModel request,
        CancellationToken cancellationToken = default)
        => StreamAsync(
            $"finding-fix-{request.ProjectKey}-{request.FindingId}",
            _promptBuilder.BuildFindingFixPrompt(request),
            _promptBuilder.BuildInferenceOptions().MaxTokens,
            null,
            cancellationToken);

    private async IAsyncEnumerable<AiStreamEventModel> StreamAsync(
        string sessionSeed,
        AssistantPromptModel prompt,
        int maxTokens,
        string? completionAppendix,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await _modelProvider.EnsureInitializedAsync(cancellationToken);
        var status = _modelProvider.GetStatus();
        if (!string.Equals(status.Status, "Ready", StringComparison.OrdinalIgnoreCase))
        {
            throw new ServiceUnavailableException(status.FailureReason ?? "The local assistant is unavailable.");
        }

        var sessionId = $"{sessionSeed}-{Guid.NewGuid():N}";
        var sequence = 0;
        yield return CreateEvent(sessionId, "Started", sequence++, null, null);

        await using var executor = await _modelProvider.CreateExecutorAsync(cancellationToken);
        var emittedMarkdown = new StringBuilder();
        var inferenceOptions = _promptBuilder.BuildInferenceOptions() with { MaxTokens = maxTokens };
        var stream = executor.InferAsync(prompt, inferenceOptions, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        Exception? failure = null;
        var cancelled = false;
        try
        {
            while (true)
            {
                string token;
                try
                {
                    if (!await stream.MoveNextAsync())
                    {
                        break;
                    }

                    token = stream.Current;
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                    break;
                }
                catch (Exception ex)
                {
                    failure = ex;
                    break;
                }

                if (!string.IsNullOrEmpty(token))
                {
                    var delta = ExtractNovelSuffix(emittedMarkdown, token);
                    if (delta.Length > 0)
                    {
                        emittedMarkdown.Append(delta);
                        yield return CreateEvent(sessionId, "Token", sequence++, delta, null);
                    }
                }
            }
        }
        finally
        {
            await stream.DisposeAsync();
        }

        if (cancelled)
        {
            _logger.LogInformation("Assistant stream {SessionId} cancelled by caller.", sessionId);
            yield return CreateEvent(sessionId, "Cancelled", sequence, null, "Generation stopped before completion.");
            yield break;
        }

        if (failure is not null)
        {
            _logger.LogError(failure, "Assistant stream {SessionId} failed.", sessionId);
            yield return CreateEvent(sessionId, "Error", sequence, null, failure.Message);
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(completionAppendix))
        {
            yield return CreateEvent(sessionId, "Token", sequence++, completionAppendix, null);
        }

        yield return CreateEvent(sessionId, "Completed", sequence, null, "Generation completed.");
    }

    private static AiStreamEventModel CreateEvent(
        string sessionId,
        string eventType,
        int sequence,
        string? markdownDelta,
        string? message)
        => new(sessionId, eventType, sequence, markdownDelta, message, DateTime.UtcNow);

    private static string ExtractNovelSuffix(StringBuilder emittedMarkdown, string chunk)
    {
        if (chunk.Length == 0)
        {
            return string.Empty;
        }

        if (emittedMarkdown.Length == 0)
        {
            return chunk;
        }

        var emitted = emittedMarkdown.ToString();
        if (chunk.StartsWith(emitted, StringComparison.Ordinal))
        {
            return chunk[emitted.Length..];
        }

        if (emitted.EndsWith(chunk, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        var maxOverlap = Math.Min(emitted.Length, chunk.Length);
        for (var overlap = maxOverlap; overlap > 0; overlap--)
        {
            if (emitted.AsSpan(emitted.Length - overlap).SequenceEqual(chunk.AsSpan(0, overlap)))
            {
                return chunk[overlap..];
            }
        }

        return chunk;
    }

    private static string? BuildProjectPlanCoverageAppendix(ProjectPlanRequestModel request)
    {
        if (request.ImpactedControllers.Count == 0)
        {
            return null;
        }

        var builder = new StringBuilder();
        builder.AppendLine();
        builder.AppendLine("## Impacted Controllers");
        builder.AppendLine();
        builder.AppendLine($"Snapshot coverage: {request.ImpactedControllers.Count} controller files across {request.TotalFindingCount} duplicate findings.");
        builder.AppendLine();

        foreach (var controller in request.ImpactedControllers)
        {
            builder.Append("- ");
            builder.AppendLine(controller);
        }

        return builder.ToString();
    }
}
