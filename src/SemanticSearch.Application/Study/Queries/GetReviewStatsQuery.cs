using MediatR;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Queries;

public sealed record GetReviewStatsQuery() : IRequest<ReviewStatsModel>;

public sealed class GetReviewStatsQueryHandler : IRequestHandler<GetReviewStatsQuery, ReviewStatsModel>
{
    private readonly IFlashCardRepository _flashCardRepository;

    public GetReviewStatsQueryHandler(IFlashCardRepository flashCardRepository)
    {
        _flashCardRepository = flashCardRepository;
    }

    public async Task<ReviewStatsModel> Handle(GetReviewStatsQuery request, CancellationToken cancellationToken)
    {
        var retentionRate = await _flashCardRepository.GetRetentionRateAsync(30, cancellationToken);
        var forecast = await _flashCardRepository.GetReviewForecastAsync(30, cancellationToken);
        var recentHistory = await _flashCardRepository.GetRecentReviewHistoryAsync(30, cancellationToken);

        return new ReviewStatsModel(
            retentionRate,
            forecast.Select(day => new ReviewForecastModel(day.Date, day.Count)).ToList(),
            recentHistory.Select(day => new DailyReviewCountModel(day.Date, day.Count, day.Accuracy)).ToList());
    }
}