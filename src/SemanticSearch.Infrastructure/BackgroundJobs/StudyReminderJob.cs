using Microsoft.Extensions.Logging;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Infrastructure.BackgroundJobs;

public sealed class StudyReminderJob
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IStudyRepository _studyRepository;
    private readonly IFlashCardRepository _flashCardRepository;
    private readonly IStudyReminderSettingsRepository _settingsRepository;
    private readonly ISlackApiClient _slackClient;
    private readonly ICredentialEncryption _encryption;
    private readonly ILogger<StudyReminderJob> _logger;

    public StudyReminderJob(
        ICredentialRepository credentialRepository,
        IStudyRepository studyRepository,
        IFlashCardRepository flashCardRepository,
        IStudyReminderSettingsRepository settingsRepository,
        ISlackApiClient slackClient,
        ICredentialEncryption encryption,
        ILogger<StudyReminderJob> logger)
    {
        _credentialRepository = credentialRepository;
        _studyRepository = studyRepository;
        _flashCardRepository = flashCardRepository;
        _settingsRepository = settingsRepository;
        _slackClient = slackClient;
        _encryption = encryption;
        _logger = logger;
    }

    public async Task Execute()
    {
        var settings = await _settingsRepository.GetAsync();
        if (settings is null || !settings.Enabled)
        {
            _logger.LogInformation("Study reminder skipped: reminders are disabled.");
            return;
        }

        var slackCred = await _credentialRepository.GetSlackCredentialAsync();
        if (slackCred is null)
        {
            _logger.LogWarning("Study reminder skipped: no Slack credentials configured.");
            return;
        }

        var planItems = await _studyRepository.GetPlanItemsByDateAsync(DateTime.UtcNow.Date);
        var duePlanItems = planItems.Count(item => item.Status is "Pending" or "InProgress");
        var dueCards = await _flashCardRepository.GetDueCardCountAsync(DateTime.UtcNow.Date);
        if (duePlanItems == 0 && dueCards == 0)
        {
            _logger.LogInformation("Study reminder skipped: nothing due today.");
            return;
        }

        var botToken = _encryption.Decrypt(slackCred.EncryptedBotToken);
        var message = $"Study reminder: {duePlanItems} chapters scheduled, {dueCards} flashcards due for review today.";
        var success = await _slackClient.PostMessageAsync(botToken, slackCred.DefaultChannel, message);
        if (success)
            _logger.LogInformation("Study reminder posted to {Channel}.", slackCred.DefaultChannel);
        else
            _logger.LogError("Failed to post study reminder to {Channel}.", slackCred.DefaultChannel);
    }
}