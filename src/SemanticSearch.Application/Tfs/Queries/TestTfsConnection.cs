using MediatR;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Tfs.Queries;

public sealed record TestTfsConnectionResult(bool Success, string? Error = null);

public sealed record TestTfsConnectionQuery : IRequest<TestTfsConnectionResult>;

public sealed class TestTfsConnectionQueryHandler : IRequestHandler<TestTfsConnectionQuery, TestTfsConnectionResult>
{
    private readonly ICredentialRepository _repo;
    private readonly ICredentialEncryption _encryption;
    private readonly ITfsApiClient _tfsClient;

    public TestTfsConnectionQueryHandler(
        ICredentialRepository repo,
        ICredentialEncryption encryption,
        ITfsApiClient tfsClient)
    {
        _repo = repo;
        _encryption = encryption;
        _tfsClient = tfsClient;
    }

    public async Task<TestTfsConnectionResult> Handle(TestTfsConnectionQuery request, CancellationToken cancellationToken)
    {
        var cred = await _repo.GetTfsCredentialAsync(cancellationToken);
        if (cred is null) return new TestTfsConnectionResult(false, "No TFS credentials configured.");

        var pat = _encryption.Decrypt(cred.EncryptedPat);
        var result = await _tfsClient.TestConnectionAsync(cred.ServerUrl, pat, cancellationToken);
        return new TestTfsConnectionResult(result.Success, result.Error);
    }
}
