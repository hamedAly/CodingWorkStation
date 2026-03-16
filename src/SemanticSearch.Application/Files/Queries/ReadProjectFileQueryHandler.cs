using MediatR;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Common.Models;

namespace SemanticSearch.Application.Files.Queries;

public sealed class ReadProjectFileQueryHandler : IRequestHandler<ReadProjectFileQuery, ProjectFileContent>
{
    private readonly IProjectFileReader _projectFileReader;

    public ReadProjectFileQueryHandler(IProjectFileReader projectFileReader)
    {
        _projectFileReader = projectFileReader;
    }

    public Task<ProjectFileContent> Handle(ReadProjectFileQuery request, CancellationToken cancellationToken)
        => _projectFileReader.ReadFileAsync(request.ProjectKey, request.RelativeFilePath, cancellationToken);
}
