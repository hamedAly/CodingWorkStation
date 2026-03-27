using MediatR;
using Microsoft.AspNetCore.Mvc;
using SemanticSearch.Application.Study.Commands;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Application.Study.Queries;
using SemanticSearch.WebApi.Contracts.Study;

namespace SemanticSearch.WebApi.Controllers;

[ApiController]
[Route("api/study")]
public sealed class StudyCardsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StudyCardsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("decks")]
    public async Task<IActionResult> ListDecks(CancellationToken cancellationToken)
        => Ok((await _mediator.Send(new ListDecksQuery(), cancellationToken)).Select(MapSummary).ToList());

    [HttpPost("decks")]
    public async Task<IActionResult> CreateDeck([FromBody] CreateDeckRequest request, CancellationToken cancellationToken)
    {
        var deck = await _mediator.Send(new CreateDeckCommand(request.Title, request.BookId), cancellationToken);
        return CreatedAtAction(nameof(GetDeck), new { deckId = deck.Id }, MapDetail(deck));
    }

    [HttpGet("decks/{deckId}")]
    public async Task<IActionResult> GetDeck([FromRoute] string deckId, CancellationToken cancellationToken)
        => Ok(MapDetail(await _mediator.Send(new GetDeckQuery(deckId), cancellationToken)));

    [HttpDelete("decks/{deckId}")]
    public async Task<IActionResult> DeleteDeck([FromRoute] string deckId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteDeckCommand(deckId), cancellationToken);
        return NoContent();
    }

    [HttpPost("decks/{deckId}/cards")]
    public async Task<IActionResult> AddCard([FromRoute] string deckId, [FromBody] AddFlashCardRequest request, CancellationToken cancellationToken)
    {
        var card = await _mediator.Send(new AddFlashCardCommand(deckId, request.Front, request.Back, request.ChapterId), cancellationToken);
        return CreatedAtAction(nameof(GetDeck), new { deckId }, MapCard(card));
    }

    [HttpPut("decks/{deckId}/cards/{cardId}")]
    public async Task<IActionResult> UpdateCard([FromRoute] string deckId, [FromRoute] string cardId, [FromBody] AddFlashCardRequest request, CancellationToken cancellationToken)
        => Ok(MapCard(await _mediator.Send(new UpdateFlashCardCommand(deckId, cardId, request.Front, request.Back, request.ChapterId), cancellationToken)));

    [HttpDelete("decks/{deckId}/cards/{cardId}")]
    public async Task<IActionResult> DeleteCard([FromRoute] string deckId, [FromRoute] string cardId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteFlashCardCommand(deckId, cardId), cancellationToken);
        return NoContent();
    }

    [HttpPost("decks/{deckId}/generate-from-chapter/{chapterId}")]
    public async Task<IActionResult> GenerateFromChapter([FromRoute] string deckId, [FromRoute] string chapterId, CancellationToken cancellationToken)
        => Ok(MapGenerated(await _mediator.Send(new GenerateCardsFromChapterCommand(deckId, chapterId), cancellationToken)));

    [HttpGet("review/due")]
    public async Task<IActionResult> Due(CancellationToken cancellationToken)
        => Ok(MapDue(await _mediator.Send(new GetDueCardsQuery(), cancellationToken)));

    [HttpPost("review/{cardId}")]
    public async Task<IActionResult> Review([FromRoute] string cardId, [FromBody] ReviewCardRequest request, CancellationToken cancellationToken)
        => Ok(MapReview(await _mediator.Send(new ReviewCardCommand(cardId, request.Quality), cancellationToken)));

    [HttpGet("review/stats")]
    public async Task<IActionResult> ReviewStats(CancellationToken cancellationToken)
        => Ok(MapStats(await _mediator.Send(new GetReviewStatsQuery(), cancellationToken)));

    private static DeckSummaryResponse MapSummary(DeckSummaryModel model)
        => new(model.Id, model.Title, model.BookId, model.BookTitle, model.TotalCards, model.DueCards);

    private static DeckDetailResponse MapDetail(DeckDetailModel model)
        => new(model.Id, model.Title, model.BookId, model.BookTitle, model.Cards.Select(MapCard).ToList(), model.DueCards);

    private static FlashCardResponse MapCard(FlashCardModel model)
        => new(model.Id, model.ChapterId, model.Front, model.Back, model.Interval, model.Repetitions, model.EaseFactor, model.NextReviewDate, model.LastReviewDate);

    private static GeneratedCardsResponse MapGenerated(GeneratedCardsModel model)
        => new(model.Cards.Select(MapCard).ToList(), model.GeneratedCount);

    private static DueCardsResponse MapDue(DueCardsModel model)
        => new(model.Cards.Select(card => new DueCardResponse(card.CardId, card.DeckId, card.DeckTitle, card.Front, card.Back, card.Interval, card.Repetitions, card.EaseFactor, card.NextReviewDate)).ToList(), model.TotalCount);

    private static ReviewResultResponse MapReview(ReviewResultModel model)
        => new(model.CardId, model.Quality, model.NewInterval, model.NewRepetitions, model.NewEaseFactor, model.NextReviewDate);

    private static ReviewStatsResponse MapStats(ReviewStatsModel model)
        => new(model.RetentionRate, model.Forecast.Select(day => new ReviewForecastDay(day.Date, day.DueCount)).ToList(), model.RecentHistory.Select(day => new DailyReviewCount(day.Date, day.Count, day.Accuracy)).ToList());
}