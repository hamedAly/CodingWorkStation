namespace SemanticSearch.Domain.Entities;

public sealed class CardReview
{
    public string Id { get; init; } = string.Empty;
    public string CardId { get; init; } = string.Empty;
    public int Quality { get; init; }
    public DateTime ReviewedAt { get; init; }
    public int PreviousInterval { get; init; }
    public int NewInterval { get; init; }
    public double PreviousEaseFactor { get; init; }
    public double NewEaseFactor { get; init; }
}
