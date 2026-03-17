using MediatR;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Quality.Assistant.Models;

namespace SemanticSearch.Application.Quality.Assistant.Queries;

public sealed class GetAssistantStatusQueryHandler : IRequestHandler<GetAssistantStatusQuery, AssistantStatusModel>
{
    private readonly IAiAssistantModelProvider _modelProvider;

    public GetAssistantStatusQueryHandler(IAiAssistantModelProvider modelProvider)
    {
        _modelProvider = modelProvider;
    }

    public Task<AssistantStatusModel> Handle(GetAssistantStatusQuery request, CancellationToken cancellationToken)
        => Task.FromResult(_modelProvider.GetStatus());
}
