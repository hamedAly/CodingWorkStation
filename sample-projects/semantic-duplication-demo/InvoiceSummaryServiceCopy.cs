namespace SemanticDuplicationDemo;
public sealed class InvoiceSummaryService
{
    public string BuildSummary(string accountId, decimal invoiceTotal, decimal paidAmount)
    {
        var outstandingBalance = CalculateOutstandingBalance(invoiceTotal, paidAmount);
        var paymentState = outstandingBalance <= 0 ? "settled" : "pending";
        var header = $"Account {accountId} invoice summary";
        var totalLine = $"Total due: {invoiceTotal:0.00}";
        var paidLine = $"Paid so far: {paidAmount:0.00}";
        var balanceLine = $"Outstanding: {outstandingBalance:0.00}";
        var statusLine = $"Status: {paymentState}";

        return string.Join(
            Environment.NewLine,
            header,
            totalLine,
            paidLine,
            balanceLine,
            statusLine);
    }

    private static decimal CalculateOutstandingBalance(decimal invoiceTotal, decimal paidAmount)
        => invoiceTotal - paidAmount;
}
