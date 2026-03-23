using MediatR;
using SemanticSearch.Application.Architecture.Models;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Architecture.Commands;

public sealed class RunDependencyAnalysisCommandHandler : IRequestHandler<RunDependencyAnalysisCommand, DependencyRunResult>
{
    private readonly IDependencyExtractor _extractor;
    private readonly IDependencyRepository _repository;

    public RunDependencyAnalysisCommandHandler(IDependencyExtractor extractor, IDependencyRepository repository)
    {
        _extractor = extractor;
        _repository = repository;
    }

    public async Task<DependencyRunResult> Handle(RunDependencyAnalysisCommand request, CancellationToken cancellationToken)
    {
        var runId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        try
        {
            var result = await _extractor.ExtractAsync(request.ProjectKey, cancellationToken);

            var run = new DependencyAnalysisRun
            {
                RunId = runId,
                ProjectKey = request.ProjectKey,
                Status = DependencyAnalysisStatus.Completed,
                RequestedUtc = now,
                StartedUtc = now,
                CompletedUtc = DateTime.UtcNow,
                TotalFilesScanned = result.FilesScanned,
                TotalNodesFound = result.Nodes.Count,
                TotalEdgesFound = result.Edges.Count
            };

            await _repository.ReplaceDependencyGraphAsync(run, result.Nodes, result.Edges, cancellationToken);

            return new DependencyRunResult(
                run.RunId, run.ProjectKey, run.Status,
                run.TotalFilesScanned, run.TotalNodesFound, run.TotalEdgesFound,
                null);
        }
        catch (Exception ex)
        {
            var failedRun = new DependencyAnalysisRun
            {
                RunId = runId,
                ProjectKey = request.ProjectKey,
                Status = DependencyAnalysisStatus.Failed,
                RequestedUtc = now,
                StartedUtc = now,
                CompletedUtc = DateTime.UtcNow,
                FailureReason = ex.Message
            };

            await _repository.ReplaceDependencyGraphAsync(failedRun, [], [], cancellationToken);

            return new DependencyRunResult(
                failedRun.RunId, failedRun.ProjectKey, failedRun.Status,
                0, 0, 0, ex.Message);
        }
    }
}
