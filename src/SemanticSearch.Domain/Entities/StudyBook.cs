namespace SemanticSearch.Domain.Entities;

public sealed class StudyBook
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Author { get; init; }
    public string? Description { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public int PageCount { get; init; }
    public int LastReadPage { get; init; } = 1;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
