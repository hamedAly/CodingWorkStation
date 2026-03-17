using MediatR;
using SemanticSearch.Application.Quality.Assistant.Models;

namespace SemanticSearch.Application.Quality.Assistant.Queries;

public sealed record GetAssistantStatusQuery() : IRequest<AssistantStatusModel>;
