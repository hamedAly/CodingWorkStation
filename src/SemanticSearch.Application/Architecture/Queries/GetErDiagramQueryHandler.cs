using MediatR;
using SemanticSearch.Application.Architecture.Models;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Architecture.Queries;

public sealed class GetErDiagramQueryHandler : IRequestHandler<GetErDiagramQuery, ErDiagramModel>
{
    private readonly IErDiagramGenerator _generator;

    public GetErDiagramQueryHandler(IErDiagramGenerator generator)
    {
        _generator = generator;
    }

    public async Task<ErDiagramModel> Handle(GetErDiagramQuery request, CancellationToken cancellationToken)
    {
        var result = await _generator.GenerateAsync(cancellationToken);
        return new ErDiagramModel(result.MermaidMarkup, result.EntityCount, result.RelationshipCount);
    }
}
