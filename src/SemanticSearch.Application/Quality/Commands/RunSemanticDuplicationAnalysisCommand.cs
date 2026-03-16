using MediatR;
using SemanticSearch.Application.Quality.Models;

namespace SemanticSearch.Application.Quality.Commands;

public sealed record RunSemanticDuplicationAnalysisCommand(string ProjectKey, string? ScopePath, double? SimilarityThreshold, int? MaxPairs) : IRequest<QualityRunResult>;
