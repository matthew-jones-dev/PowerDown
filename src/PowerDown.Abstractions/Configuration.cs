namespace PowerDown.Abstractions;

public class Configuration
{
    private int _verificationDelaySeconds = 120;
    private int _pollingIntervalSeconds = 15;
    private int _requiredNoActivityChecks = 5;
    private int _shutdownDelaySeconds = 60;

    public int VerificationDelaySeconds
    {
        get => _verificationDelaySeconds;
        set => _verificationDelaySeconds = value > 0 ? value : throw new ArgumentException("Verification delay must be greater than 0", nameof(VerificationDelaySeconds));
    }

    public int PollingIntervalSeconds
    {
        get => _pollingIntervalSeconds;
        set => _pollingIntervalSeconds = value > 0 ? value : throw new ArgumentException("Polling interval must be greater than 0", nameof(PollingIntervalSeconds));
    }

    public int RequiredNoActivityChecks
    {
        get => _requiredNoActivityChecks;
        set => _requiredNoActivityChecks = value > 0 ? value : throw new ArgumentException("Required checks must be greater than 0", nameof(RequiredNoActivityChecks));
    }

    /// <summary>
    /// Delay in seconds before system shutdown after downloads complete.
    /// Default is 30 seconds.
    /// </summary>
    public int ShutdownDelaySeconds
    {
        get => _shutdownDelaySeconds;
        set => _shutdownDelaySeconds = value > 0 ? value : throw new ArgumentException("Shutdown delay must be greater than 0", nameof(ShutdownDelaySeconds));
    }

    public bool MonitorSteam { get; set; } = true;
    public bool DryRun { get; set; }
    public bool Verbose { get; set; }
    public string? CustomSteamPath { get; set; }
}
