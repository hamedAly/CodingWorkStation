using MediatR;
using SemanticSearch.Application.Quality.Assistant.Models;

namespace SemanticSearch.Application.Quality.Assistant.Queries;

public sealed record StreamFindingFixQuery(string ProjectKey, string FindingId) : IRequest<FindingFixRequestModel>;
