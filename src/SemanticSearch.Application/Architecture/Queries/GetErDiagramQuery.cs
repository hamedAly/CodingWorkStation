using MediatR;
using SemanticSearch.Application.Architecture.Models;

namespace SemanticSearch.Application.Architecture.Queries;

public sealed record GetErDiagramQuery() : IRequest<ErDiagramModel>;
