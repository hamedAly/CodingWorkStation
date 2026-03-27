using SemanticSearch.Domain.Entities;

namespace SemanticSearch.Domain.Interfaces;

public interface IFlashCardRepository
{
    Task<IReadOnlyList<FlashCardDeck>> GetAllDecksAsync(CancellationToken cancellationToken = default);
    Task<FlashCardDeck?> GetDeckByIdAsync(string deckId, CancellationToken cancellationToken = default);
    Task InsertDeckAsync(FlashCardDeck deck, CancellationToken cancellationToken = default);
    Task UpdateDeckAsync(FlashCardDeck deck, CancellationToken cancellationToken = default);
    Task DeleteDeckAsync(string deckId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FlashCard>> GetCardsByDeckIdAsync(string deckId, CancellationToken cancellationToken = default);
    Task<FlashCard?> GetCardByIdAsync(string cardId, CancellationToken cancellationToken = default);
    Task InsertCardAsync(FlashCard card, CancellationToken cancellationToken = default);
    Task InsertCardsAsync(IReadOnlyList<FlashCard> cards, CancellationToken cancellationToken = default);
    Task UpdateCardAsync(FlashCard card, CancellationToken cancellationToken = default);
    Task DeleteCardAsync(string cardId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FlashCard>> GetDueCardsAsync(DateTime asOfDate, CancellationToken cancellationToken = default);
    Task<int> GetDueCardCountAsync(DateTime asOfDate, CancellationToken cancellationToken = default);

    Task InsertReviewAsync(CardReview review, CancellationToken cancellationToken = default);
    Task<double> GetRetentionRateAsync(int lastNDays, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(DateTime Date, int Count)>> GetReviewForecastAsync(int days, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(DateTime Date, int Count, double Accuracy)>> GetRecentReviewHistoryAsync(int days, CancellationToken cancellationToken = default);
}
