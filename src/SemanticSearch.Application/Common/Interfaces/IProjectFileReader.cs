using SemanticSearch.Application.Common.Models;

namespace SemanticSearch.Application.Common.Interfaces;

public interface IProjectFileReader
{
    Task<ProjectFileContent> ReadFileAsync(
        string projectKey,
        string relativeFilePath,
        CancellationToken cancellationToken = default);
}
