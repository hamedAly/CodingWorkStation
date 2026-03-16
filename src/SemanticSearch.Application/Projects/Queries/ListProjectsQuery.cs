using MediatR;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Projects.Queries;

public sealed record ListProjectsQuery() : IRequest<IReadOnlyList<IndexingStatus>>;
