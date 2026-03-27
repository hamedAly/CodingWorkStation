namespace SemanticSearch.Domain.Entities;

public sealed class FlashCard
{
    public string Id { get; init; } = string.Empty;
    public string DeckId { get; init; } = string.Empty;
    public string? ChapterId { get; init; }
    public string Front { get; init; } = string.Empty;
    public string Back { get; init; } = string.Empty;
    public int Interval { get; init; }
    public int Repetitions { get; init; }
    public double EaseFactor { get; init; } = 2.5d;
    public DateTime NextReviewDate { get; init; }
    public DateTime? LastReviewDate { get; init; }
    public DateTime CreatedAt { get; init; }
}
