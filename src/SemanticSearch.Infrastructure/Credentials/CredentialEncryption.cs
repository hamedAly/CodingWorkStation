using Microsoft.AspNetCore.DataProtection;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Infrastructure.Credentials;

public sealed class CredentialEncryption : ICredentialEncryption
{
    private readonly IDataProtector _protector;

    public CredentialEncryption(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("SemanticSearch.Credentials");
    }

    public string Encrypt(string plainText) => _protector.Protect(plainText);

    public string Decrypt(string cipherText) => _protector.Unprotect(cipherText);
}
