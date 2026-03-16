using MediatR;
using SemanticSearch.Application.Common.Models;

namespace SemanticSearch.Application.Files.Queries;

public sealed record ReadProjectFileQuery(string ProjectKey, string RelativeFilePath) : IRequest<ProjectFileContent>;
