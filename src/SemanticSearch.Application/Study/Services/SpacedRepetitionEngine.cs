namespace SemanticSearch.Application.Study.Services;

public static class SpacedRepetitionEngine
{
    public static (int NewInterval, int NewRepetitions, double NewEaseFactor, DateTime NextReviewDate) Calculate(
        int quality,
        int currentInterval,
        int repetitions,
        double easeFactor)
    {
        if (quality is < 0 or > 5)
            throw new ArgumentOutOfRangeException(nameof(quality), "Quality must be between 0 and 5.");

        var normalizedEaseFactor = Math.Max(1.3d, easeFactor);
        var newEaseFactor = normalizedEaseFactor + (0.1d - (5 - quality) * (0.08d + (5 - quality) * 0.02d));
        newEaseFactor = Math.Max(1.3d, Math.Round(newEaseFactor, 2, MidpointRounding.AwayFromZero));

        int newInterval;
        int newRepetitions;

        if (quality >= 3)
        {
            newRepetitions = repetitions + 1;
            newInterval = repetitions switch
            {
                <= 0 => 1,
                1 => 6,
                _ => Math.Max(1, (int)Math.Round(currentInterval * normalizedEaseFactor, MidpointRounding.AwayFromZero))
            };
        }
        else
        {
            newRepetitions = 0;
            newInterval = 1;
        }

        var nextReviewDate = DateTime.UtcNow.Date.AddDays(newInterval);
        return (newInterval, newRepetitions, newEaseFactor, nextReviewDate);
    }
}
