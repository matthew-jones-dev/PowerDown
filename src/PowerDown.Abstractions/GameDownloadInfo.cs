namespace PowerDown.Abstractions;

public class GameDownloadInfo
{
    public string GameName { get; set; } = string.Empty;
    public DownloadStatus DownloadStatus { get; set; }
    public DownloadStatus InstallStatus { get; set; }
    public double Progress { get; set; }
    public string LauncherName { get; set; } = string.Empty;
}
