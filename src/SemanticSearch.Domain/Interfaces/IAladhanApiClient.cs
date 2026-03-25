namespace SemanticSearch.Domain.Interfaces;

public sealed record PrayerTimesResult(
    TimeOnly Fajr,
    TimeOnly Dhuhr,
    TimeOnly Asr,
    TimeOnly Maghrib,
    TimeOnly Isha);

public interface IAladhanApiClient
{
    Task<PrayerTimesResult?> GetPrayerTimesAsync(string city, string country, int method, CancellationToken cancellationToken = default);
}
