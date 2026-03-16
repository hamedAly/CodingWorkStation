namespace SemanticSearch.WebApi.Contracts.Files;

public sealed record ReadFileRequest(string ProjectKey, string RelativeFilePath);

public sealed record ReadFileResponse(
    string ProjectKey,
    string RelativeFilePath,
    string Content,
    DateTime? LastModifiedUtc);
