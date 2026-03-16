using MediatR;
using SemanticSearch.Application.Quality.Models;

namespace SemanticSearch.Application.Quality.Queries;

public sealed record GetDuplicateComparisonQuery(string ProjectKey, string FindingId) : IRequest<DuplicateComparisonModel>;
