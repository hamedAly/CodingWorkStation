using MediatR;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Application.Study.Services;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Commands;

public sealed record ReviewCardCommand(string CardId, int Quality) : IRequest<ReviewResultModel>;

public sealed class ReviewCardCommandHandler : IRequestHandler<ReviewCardCommand, ReviewResultModel>
{
    private readonly IFlashCardRepository _flashCardRepository;

    public ReviewCardCommandHandler(IFlashCardRepository flashCardRepository)
    {
        _flashCardRepository = flashCardRepository;
    }

    public async Task<ReviewResultModel> Handle(ReviewCardCommand request, CancellationToken cancellationToken)
    {
        var existingCard = await _flashCardRepository.GetCardByIdAsync(request.CardId, cancellationToken)
            ?? throw new NotFoundException($"Flashcard '{request.CardId}' was not found.");

        var review = SpacedRepetitionEngine.Calculate(request.Quality, existingCard.Interval, existingCard.Repetitions, existingCard.EaseFactor);
        var reviewedAt = DateTime.UtcNow;

        await _flashCardRepository.InsertReviewAsync(new CardReview
        {
            Id = Guid.NewGuid().ToString("N"),
            CardId = existingCard.Id,
            Quality = request.Quality,
            ReviewedAt = reviewedAt,
            PreviousInterval = existingCard.Interval,
            NewInterval = review.NewInterval,
            PreviousEaseFactor = existingCard.EaseFactor,
            NewEaseFactor = review.NewEaseFactor
        }, cancellationToken);

        await _flashCardRepository.UpdateCardAsync(new FlashCard
        {
            Id = existingCard.Id,
            DeckId = existingCard.DeckId,
            ChapterId = existingCard.ChapterId,
            Front = existingCard.Front,
            Back = existingCard.Back,
            Interval = review.NewInterval,
            Repetitions = review.NewRepetitions,
            EaseFactor = review.NewEaseFactor,
            NextReviewDate = review.NextReviewDate,
            LastReviewDate = reviewedAt,
            CreatedAt = existingCard.CreatedAt
        }, cancellationToken);

        return new ReviewResultModel(existingCard.Id, request.Quality, review.NewInterval, review.NewRepetitions, review.NewEaseFactor, review.NextReviewDate);
    }
}