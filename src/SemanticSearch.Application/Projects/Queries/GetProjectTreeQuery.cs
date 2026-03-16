using MediatR;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Projects.Queries;

public sealed record GetProjectTreeQuery(string ProjectKey) : IRequest<IReadOnlyList<ProjectTreeNode>>;
