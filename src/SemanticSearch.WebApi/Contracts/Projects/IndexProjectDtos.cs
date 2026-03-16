namespace SemanticSearch.WebApi.Contracts.Projects;

public sealed record IndexProjectRequest(string ProjectPath, string ProjectKey);

public sealed record IndexAcceptedResponse(string ProjectKey, string RunId, string Status, string Message);

public sealed record RefreshProjectFileRequest(string ProjectKey, string RelativeFilePath);
