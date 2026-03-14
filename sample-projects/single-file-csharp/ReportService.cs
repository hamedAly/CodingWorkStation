namespace SampleProject;

public sealed class ReportService
{
    public string GenerateMonthlyReport(string customerId)
    {
        var reportTitle = $"Monthly report for {customerId}";
        var pdfContent = RenderPdf(reportTitle);
        return pdfContent;
    }

    private static string RenderPdf(string title)
    {
        return $"PDF export created for: {title}";
    }
}
