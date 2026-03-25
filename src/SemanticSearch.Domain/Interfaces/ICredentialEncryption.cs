namespace SemanticSearch.Domain.Interfaces;

public interface ICredentialEncryption
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
