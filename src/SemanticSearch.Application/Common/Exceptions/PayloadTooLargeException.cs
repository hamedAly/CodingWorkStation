namespace SemanticSearch.Application.Common.Exceptions;

public sealed class PayloadTooLargeException : Exception
{
    public PayloadTooLargeException(string message)
        : base(message)
    {
    }
}
