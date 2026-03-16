using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Common.Models;
using SemanticSearch.Infrastructure.Common;

namespace SemanticSearch.Infrastructure.FileSystem;

public sealed class ProjectFileReader : IProjectFileReader
{
    private readonly IProjectWorkspaceRepository _workspaceRepository;
    private readonly ProjectCatalogService _projectCatalogService;

    public ProjectFileReader(
        IProjectWorkspaceRepository workspaceRepository,
        ProjectCatalogService projectCatalogService)
    {
        _workspaceRepository = workspaceRepository;
        _projectCatalogService = projectCatalogService;
    }

    public async Task<ProjectFileContent> ReadFileAsync(
        string projectKey,
        string relativeFilePath,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspaceRepository.GetAsync(projectKey, cancellationToken);
        if (workspace is null)
            throw new NotFoundException($"Project '{projectKey}' was not found.");

        var absoluteFilePath = _projectCatalogService.ToAbsolutePath(workspace.SourceRootPath, relativeFilePath);
        if (!File.Exists(absoluteFilePath))
            throw new NotFoundException($"File '{relativeFilePath}' was not found.");

        var result = await TextFileLoader.TryReadSanitizedTextAsync(absoluteFilePath, cancellationToken);
        if (!result.Success)
        {
            var message = result.IsBinary
                ? $"File '{relativeFilePath}' is not a readable text file."
                : $"File '{relativeFilePath}' could not be read: {result.FailureReason}";

            throw new ConflictException(message);
        }

        return new ProjectFileContent(
            projectKey,
            relativeFilePath,
            result.Content,
            File.GetLastWriteTimeUtc(absoluteFilePath));
    }
}
