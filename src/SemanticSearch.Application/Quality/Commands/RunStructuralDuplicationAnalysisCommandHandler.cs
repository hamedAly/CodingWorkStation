using MediatR;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Quality.Models;

namespace SemanticSearch.Application.Quality.Commands;

public sealed class RunStructuralDuplicationAnalysisCommandHandler : IRequestHandler<RunStructuralDuplicationAnalysisCommand, QualityRunResult>
{
    private readonly IQualityRunCoordinator _qualityRunCoordinator;

    public RunStructuralDuplicationAnalysisCommandHandler(IQualityRunCoordinator qualityRunCoordinator)
    {
        _qualityRunCoordinator = qualityRunCoordinator;
    }

    public async Task<QualityRunResult> Handle(RunStructuralDuplicationAnalysisCommand request, CancellationToken cancellationToken)
    {
        var snapshot = await _qualityRunCoordinator.GenerateSnapshotAsync(
            request.ProjectKey,
            includeStructural: true,
            includeSemantic: false,
            scopePath: request.ScopePath,
            minimumStructuralLines: request.MinimumLines,
            cancellationToken: cancellationToken);

        return QualityReadModelMapper.ToRunResult(snapshot, "Structural");
    }
}
