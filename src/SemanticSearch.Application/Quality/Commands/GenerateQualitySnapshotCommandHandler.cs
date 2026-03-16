using MediatR;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Quality.Models;

namespace SemanticSearch.Application.Quality.Commands;

public sealed class GenerateQualitySnapshotCommandHandler : IRequestHandler<GenerateQualitySnapshotCommand, QualitySnapshotResult>
{
    private readonly IQualityRunCoordinator _qualityRunCoordinator;

    public GenerateQualitySnapshotCommandHandler(IQualityRunCoordinator qualityRunCoordinator)
    {
        _qualityRunCoordinator = qualityRunCoordinator;
    }

    public Task<QualitySnapshotResult> Handle(GenerateQualitySnapshotCommand request, CancellationToken cancellationToken)
        => _qualityRunCoordinator.GenerateSnapshotAsync(
            request.ProjectKey,
            true,
            true,
            scopePath: request.ScopePath,
            cancellationToken: cancellationToken);
}
