using MediatR;
using SemanticSearch.Application.Architecture.Models;

namespace SemanticSearch.Application.Architecture.Commands;

public sealed record RunDependencyAnalysisCommand(string ProjectKey) : IRequest<DependencyRunResult>;
