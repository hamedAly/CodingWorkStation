using MediatR;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Projects.Queries;

public sealed class GetProjectTreeQueryHandler : IRequestHandler<GetProjectTreeQuery, IReadOnlyList<ProjectTreeNode>>
{
    private readonly IProjectTreeService _projectTreeService;

    public GetProjectTreeQueryHandler(IProjectTreeService projectTreeService)
    {
        _projectTreeService = projectTreeService;
    }

    public Task<IReadOnlyList<ProjectTreeNode>> Handle(GetProjectTreeQuery request, CancellationToken cancellationToken)
        => _projectTreeService.GetTreeAsync(request.ProjectKey, cancellationToken);
}
