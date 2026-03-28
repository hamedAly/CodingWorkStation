namespace SemanticSearch.Domain.Entities;

public sealed class StudyChapter
{
    public string Id { get; init; } = string.Empty;
    public string BookId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int StartPage { get; init; }
    public int EndPage { get; init; }
    public int SortOrder { get; init; }
    public string? AudioFileName { get; init; }
    public string? AudioFilePath { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
}
