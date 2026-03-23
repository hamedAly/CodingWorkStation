using MediatR;
using SemanticSearch.Application.Architecture.Models;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Architecture.Queries;

public sealed class GetFileHeatmapQueryHandler : IRequestHandler<GetFileHeatmapQuery, FileHeatmapModel?>
{
    private readonly IHeatmapDataBuilder _heatmapDataBuilder;
    private readonly IQualityRepository _qualityRepository;

    public GetFileHeatmapQueryHandler(IHeatmapDataBuilder heatmapDataBuilder, IQualityRepository qualityRepository)
    {
        _heatmapDataBuilder = heatmapDataBuilder;
        _qualityRepository = qualityRepository;
    }

    public async Task<FileHeatmapModel?> Handle(GetFileHeatmapQuery request, CancellationToken cancellationToken)
    {
        var run = await _qualityRepository.GetLatestAnalysisRunAsync(request.ProjectKey, cancellationToken);
        if (run is null) return null;

        var entries = await _heatmapDataBuilder.BuildAsync(request.ProjectKey, cancellationToken);
        return new FileHeatmapModel(request.ProjectKey, entries.Count, entries);
    }
}
