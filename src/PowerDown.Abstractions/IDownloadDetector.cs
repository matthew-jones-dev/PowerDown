namespace PowerDown.Abstractions;

public interface IDownloadDetector
{
    string LauncherName { get; }
    Task<IEnumerable<GameDownloadInfo>> GetActiveDownloadsAsync();
    Task<bool> IsAnyDownloadOrInstallActiveAsync();
    Task<bool> InitializeAsync();
}
