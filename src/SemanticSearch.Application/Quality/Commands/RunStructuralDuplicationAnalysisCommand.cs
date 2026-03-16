using MediatR;
using SemanticSearch.Application.Quality.Models;

namespace SemanticSearch.Application.Quality.Commands;

public sealed record RunStructuralDuplicationAnalysisCommand(string ProjectKey, string? ScopePath, int? MinimumLines) : IRequest<QualityRunResult>;
