namespace SemanticSearch.Application.Common;

public sealed class IntegrationOptions
{
    public string StandupCron { get; set; } = "30 9 * * MON-FRI";
    public string PrayerFetchCron { get; set; } = "0 0 * * *";
    public int DefaultPrayerMethod { get; set; } = 4;
    public string TfsApiVersion { get; set; } = "7.1";
    /// <summary>
    /// Disable TLS certificate validation for on-premises TFS/Azure DevOps Server
    /// instances that use self-signed or internal CA certificates.
    /// Set to true only for trusted internal servers.
    /// </summary>
    public bool IgnoreTlsErrors { get; init; } = false;}
