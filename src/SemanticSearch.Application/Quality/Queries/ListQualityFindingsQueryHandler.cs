using MediatR;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Quality.Models;

namespace SemanticSearch.Application.Quality.Queries;

public sealed class ListQualityFindingsQueryHandler : IRequestHandler<ListQualityFindingsQuery, QualityFindingsResult>
{
    private readonly IProjectFileRepository _projectFileRepository;
    private readonly IQualityRepository _qualityRepository;
    private readonly IQualityRunCoordinator _qualityRunCoordinator;
    private readonly QualityRefreshPolicy _refreshPolicy;

    public ListQualityFindingsQueryHandler(
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

    public async Task<QualityFindingsResult> Handle(ListQualityFindingsQuery request, CancellationToken cancellationToken)
    {
        var summary = await _qualityRepository.GetSummaryAsync(request.ProjectKey, cancellationToken);
        var files = await _projectFileRepository.ListFilesAsync(request.ProjectKey, cancellationToken);

        if (summary is null || _refreshPolicy.ShouldRefresh(summary, files))
        {
            var snapshot = await _qualityRunCoordinator.GenerateSnapshotAsync(request.ProjectKey, true, true, cancellationToken: cancellationToken);
            return new QualityFindingsResult(snapshot.ProjectKey, snapshot.RunId, snapshot.Findings);
        }

        var findings = await _qualityRepository.ListFindingsAsync(request.ProjectKey, cancellationToken);
        var results = new List<QualityFindingModel>();
        foreach (var finding in findings)
        {
            var left = await _qualityRepository.GetRegionAsync(request.ProjectKey, finding.LeftRegionId, cancellationToken);
            var right = await _qualityRepository.GetRegionAsync(request.ProjectKey, finding.RightRegionId, cancellationToken);
            if (left is null || right is null)
            {
                continue;
            }

            results.Add(QualityReadModelMapper.MapFinding(finding, left, right));
        }

        return new QualityFindingsResult(request.ProjectKey, summary.RunId, results);
    }
}
