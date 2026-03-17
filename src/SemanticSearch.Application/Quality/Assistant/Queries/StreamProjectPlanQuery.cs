using MediatR;
using SemanticSearch.Application.Quality.Assistant.Models;

namespace SemanticSearch.Application.Quality.Assistant.Queries;

public sealed record StreamProjectPlanQuery(string ProjectKey, string RunId) : IRequest<ProjectPlanRequestModel>;
