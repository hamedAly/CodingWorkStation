using MediatR;
using SemanticSearch.Application.Architecture.Models;

namespace SemanticSearch.Application.Architecture.Queries;

public sealed record GetDependencyGraphQuery(
    string ProjectKey,
    string? Namespace = null,
    string? FilePath = null) : IRequest<DependencyGraphModel?>;
