using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Infrastructure.Slack;

public sealed class AladhanApiClient : IAladhanApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AladhanApiClient> _logger;

    public AladhanApiClient(IHttpClientFactory httpClientFactory, ILogger<AladhanApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<PrayerTimesResult?> GetPrayerTimesAsync(
        string city, string country, int method,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AladhanClient");
            var today = DateTime.UtcNow.ToString("dd-MM-yyyy");
            var url = $"v1/timingsByCity/{today}?city={Uri.EscapeDataString(city)}&country={Uri.EscapeDataString(country)}&method={method}";
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var node = JsonNode.Parse(json);
            var timings = node?["data"]?["timings"];
            if (timings is null) return null;

            return new PrayerTimesResult(
                Fajr: ParseTime(timings["Fajr"]?.GetValue<string>()),
                Dhuhr: ParseTime(timings["Dhuhr"]?.GetValue<string>()),
                Asr: ParseTime(timings["Asr"]?.GetValue<string>()),
                Maghrib: ParseTime(timings["Maghrib"]?.GetValue<string>()),
                Isha: ParseTime(timings["Isha"]?.GetValue<string>())
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve prayer times for {City}, {Country}.", city, country);
            return null;
        }
    }

    private static TimeOnly ParseTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return TimeOnly.MinValue;
        // Aladhan returns times as "HH:mm" or "HH:mm (timezone)"
        var clean = value.Split(' ')[0];
        return TimeOnly.TryParse(clean, out var t) ? t : TimeOnly.MinValue;
    }
}
