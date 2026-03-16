using MediatR;
using SemanticSearch.Application.Quality.Models;

namespace SemanticSearch.Application.Quality.Queries;

public sealed record ListQualityFindingsQuery(string ProjectKey) : IRequest<QualityFindingsResult>;
