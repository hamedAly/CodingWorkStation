using MediatR;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Slack.Queries;

public sealed record TestSlackConnectionResult(bool Success, string? Error = null);

public sealed record TestSlackConnectionQuery : IRequest<TestSlackConnectionResult>;

public sealed class TestSlackConnectionQueryHandler : IRequestHandler<TestSlackConnectionQuery, TestSlackConnectionResult>
{
    private readonly ICredentialRepository _repo;
    private readonly ICredentialEncryption _encryption;
    private readonly ISlackApiClient _slackClient;

    public TestSlackConnectionQueryHandler(
        ICredentialRepository repo,
        ICredentialEncryption encryption,
        ISlackApiClient slackClient)
    {
        _repo = repo;
        _encryption = encryption;
        _slackClient = slackClient;
    }

    public async Task<TestSlackConnectionResult> Handle(TestSlackConnectionQuery request, CancellationToken cancellationToken)
    {
        var cred = await _repo.GetSlackCredentialAsync(cancellationToken);
        if (cred is null) return new TestSlackConnectionResult(false, "No Slack credentials configured.");

        var botToken = _encryption.Decrypt(cred.EncryptedBotToken);
        var success = await _slackClient.TestConnectionAsync(botToken, cancellationToken);
        return success
            ? new TestSlackConnectionResult(true)
            : new TestSlackConnectionResult(false, "Connection failed. Verify bot token.");
    }
}
