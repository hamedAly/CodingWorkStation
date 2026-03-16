using MediatR;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Quality.Models;

namespace SemanticSearch.Application.Quality.Commands;

public sealed class RunSemanticDuplicationAnalysisCommandHandler : IRequestHandler<RunSemanticDuplicationAnalysisCommand, QualityRunResult>
{
    private readonly IQualityRunCoordinator _qualityRunCoordinator;

    public RunSemanticDuplicationAnalysisCommandHandler(IQualityRunCoordinator qualityRunCoordinator)
    {
        _qualityRunCoordinator = qualityRunCoordinator;
    }

    public async Task<QualityRunResult> Handle(RunSemanticDuplicationAnalysisCommand request, CancellationToken cancellationToken)
    {
        var snapshot = await _qualityRunCoordinator.GenerateSnapshotAsync(
            request.ProjectKey,
            includeStructural: false,
            includeSemantic: true,
            scopePath: request.ScopePath,
            semanticThreshold: request.SimilarityThreshold,
            maxPairs: request.MaxPairs,
            cancellationToken: cancellationToken);

        return QualityReadModelMapper.ToRunResult(snapshot, "Semantic");
    }
}
