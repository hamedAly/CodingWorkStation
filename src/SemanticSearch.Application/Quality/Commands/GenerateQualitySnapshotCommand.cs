using MediatR;
using SemanticSearch.Application.Quality.Models;

namespace SemanticSearch.Application.Quality.Commands;

public sealed record GenerateQualitySnapshotCommand(string ProjectKey, string? ScopePath) : IRequest<QualitySnapshotResult>;
