using MediatR;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Status.Queries;

public sealed class GetProjectStatusQueryHandler : IRequestHandler<GetProjectStatusQuery, GetProjectStatusResponse>
{
    private readonly IVectorStore _vectorStore;

    public GetProjectStatusQueryHandler(IVectorStore vectorStore)
    {
        _vectorStore = vectorStore;
    }

    public async Task<GetProjectStatusResponse> Handle(GetProjectStatusQuery request, CancellationToken cancellationToken)
    {
        var metadata = await _vectorStore.GetProjectMetadataAsync(request.ProjectKey, cancellationToken);

        if (metadata is null)
            return new GetProjectStatusResponse(IsIndexed: false, TotalFiles: 0, TotalChunks: 0, LastUpdated: null);

        return new GetProjectStatusResponse(
            IsIndexed: true,
            TotalFiles: metadata.TotalFiles,
            TotalChunks: metadata.TotalChunks,
            LastUpdated: metadata.LastUpdated);
    }
}
