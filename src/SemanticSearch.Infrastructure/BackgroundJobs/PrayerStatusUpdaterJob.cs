using Microsoft.Extensions.Logging;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Infrastructure.BackgroundJobs;

public sealed class PrayerStatusUpdaterJob
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly ISlackApiClient _slackClient;
    private readonly ICredentialEncryption _encryption;
    private readonly ILogger<PrayerStatusUpdaterJob> _logger;

    public PrayerStatusUpdaterJob(
        ICredentialRepository credentialRepository,
        ISlackApiClient slackClient,
        ICredentialEncryption encryption,
        ILogger<PrayerStatusUpdaterJob> logger)
    {
        _credentialRepository = credentialRepository;
        _slackClient = slackClient;
        _encryption = encryption;
        _logger = logger;
    }

    public async Task Execute(string prayerName)
    {
        var slackCred = await _credentialRepository.GetSlackCredentialAsync();
        if (slackCred?.EncryptedUserToken is null)
        {
            _logger.LogWarning("Prayer status updater skipped: no Slack user token configured.");
            return;
        }

        var userToken = _encryption.Decrypt(slackCred.EncryptedUserToken);
        var expiration = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();

        var success = await _slackClient.SetUserStatusAsync(
            userToken,
            $"Praying ({prayerName})",
            ":mosque:",
            expiration);

        if (success)
            _logger.LogInformation("Slack status set to Praying ({Prayer}) with 30-min expiration.", prayerName);
        else
            _logger.LogError("Failed to set Slack prayer status for {Prayer}.", prayerName);
    }
}
