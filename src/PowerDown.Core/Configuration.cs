namespace PowerDown.Core;

public class Configuration
{
    public int VerificationDelaySeconds { get; set; } = 60;
    public int PollingIntervalSeconds { get; set; } = 10;
    public int RequiredNoActivityChecks { get; set; } = 3;
    public bool MonitorSteam { get; set; } = true;
    public bool MonitorEpic { get; set; } = true;
    public bool DryRun { get; set; }
    public bool Verbose { get; set; }
    public string? CustomSteamPath { get; set; }
    public string? CustomEpicPath { get; set; }
}
