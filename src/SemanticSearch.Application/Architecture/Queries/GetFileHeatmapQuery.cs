using MediatR;
using SemanticSearch.Application.Architecture.Models;

namespace SemanticSearch.Application.Architecture.Queries;

public sealed record GetFileHeatmapQuery(string ProjectKey) : IRequest<FileHeatmapModel?>;
