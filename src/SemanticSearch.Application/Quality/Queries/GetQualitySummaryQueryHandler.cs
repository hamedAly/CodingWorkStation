using MediatR;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Quality.Models;

namespace SemanticSearch.Application.Quality.Queries;

public sealed class GetQualitySummaryQueryHandler : IRequestHandler<GetQualitySummaryQuery, QualitySummaryModel>
{
    private readonly IProjectFileRepository _projectFileRepository;
    private readonly IQualityRepository _qualityRepository;
    private readonly IQualityRunCoordinator _qualityRunCoordinator;
    private readonly QualityRefreshPolicy _refreshPolicy;

    public GetQualitySummaryQueryHandler(
        IProjectFileRepository projectFileRepository,
        IQualityRepository qualityRepository,
        IQualityRunCoordinator qualityRunCoordinator,
        QualityRefreshPolicy refreshPolicy)
    {
        _projectFileRepository = projectFileRepository;
        _qualityRepository = qualityRepository;
        _qualityRunCoordinator = qualityRunCoordinator;
        _refreshPolicy = refreshPolicy;
    }

    public async Task<QualitySummaryModel> Handle(GetQualitySummaryQuery request, CancellationToken cancellationToken)
    {
        var summary = await _qualityRepository.GetSummaryAsync(request.ProjectKey, cancellationToken);
        var files = await _projectFileRepository.ListFilesAsync(request.ProjectKey, cancellationToken);

        if (summary is null || _refreshPolicy.ShouldRefresh(summary, files))
        {
            var snapshot = await _qualityRunCoordinator.GenerateSnapshotAsync(request.ProjectKey, true, true, cancellationToken: cancellationToken);
            return snapshot.Summary;
        }

        return QualityReadModelMapper.MapSummary(summary);
    }
}
