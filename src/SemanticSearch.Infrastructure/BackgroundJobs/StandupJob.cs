using Microsoft.Extensions.Logging;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Infrastructure.BackgroundJobs;

public sealed class StandupJob
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IIntegrationSettingsRepository _settingsRepository;
    private readonly ISlackApiClient _slackClient;
    private readonly ICredentialEncryption _encryption;
    private readonly ILogger<StandupJob> _logger;

    public StandupJob(
        ICredentialRepository credentialRepository,
        IIntegrationSettingsRepository settingsRepository,
        ISlackApiClient slackClient,
        ICredentialEncryption encryption,
        ILogger<StandupJob> logger)
    {
        _credentialRepository = credentialRepository;
        _settingsRepository = settingsRepository;
        _slackClient = slackClient;
        _encryption = encryption;
        _logger = logger;
    }

    public async Task Execute()
    {
        var settings = await _settingsRepository.GetAsync();
        if (settings is null || !settings.StandupEnabled)
        {
            _logger.LogInformation("Standup job skipped: not enabled or no settings configured.");
            return;
        }

        var slackCred = await _credentialRepository.GetSlackCredentialAsync();
        if (slackCred is null)
        {
            _logger.LogWarning("Standup job skipped: no Slack credentials configured.");
            return;
        }

        var botToken = _encryption.Decrypt(slackCred.EncryptedBotToken);
        var success = await _slackClient.PostMessageAsync(botToken, slackCred.DefaultChannel, settings.StandupMessage);
        if (success)
            _logger.LogInformation("Standup message posted to {Channel}.", slackCred.DefaultChannel);
        else
            _logger.LogError("Failed to post standup message to {Channel}.", slackCred.DefaultChannel);
    }
}
