using SemanticSearch.Application.Quality.Assistant.Models;

namespace SemanticSearch.Application.Common.Interfaces;

public interface IQualityAssistantService
{
    IAsyncEnumerable<AiStreamEventModel> StreamProjectPlanAsync(
        ProjectPlanRequestModel request,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<AiStreamEventModel> StreamFindingFixAsync(
        FindingFixRequestModel request,
        CancellationToken cancellationToken = default);
}
