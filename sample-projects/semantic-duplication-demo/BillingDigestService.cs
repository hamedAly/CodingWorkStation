namespace SemanticDuplicationDemo;

public sealed class BillingDigestService
{
    public string CreateDigest(string customerKey, decimal billedAmount, decimal receivedAmount)
    {
        var remainingBalance = billedAmount - receivedAmount;
        var collectionState = remainingBalance <= 0 ? "settled" : "pending";
        var heading = $"Account {customerKey} invoice summary";
        var billedLine = $"Total due: {billedAmount:0.00}";
        var receivedLine = $"Paid so far: {receivedAmount:0.00}";
        var remainingLine = $"Outstanding: {remainingBalance:0.00}";
        var stateLine = $"Status: {collectionState}";

        return string.Join(
            Environment.NewLine,
            heading,
            billedLine,
            receivedLine,
            remainingLine,
            stateLine);
    }
}
