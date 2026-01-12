using System.Collections.Generic;
using System.Threading.Tasks;
using PowerDown.Abstractions;

namespace PowerDown.Core.Tests.Helpers;

public class MockDownloadDetector : IDownloadDetector
{
    private List<GameDownloadInfo> _downloads;
    private bool _isActive;
    private bool _initialized;

    public string LauncherName => "Mock Launcher";

    public MockDownloadDetector(List<GameDownloadInfo> downloads, bool isActive = false)
    {
        _downloads = downloads;
        _isActive = isActive;
    }

    public MockDownloadDetector(bool isActive = false)
    {
        _downloads = new List<GameDownloadInfo>();
        _isActive = isActive;
    }

    public Task<IEnumerable<GameDownloadInfo>> GetActiveDownloadsAsync()
    {
        return Task.FromResult<IEnumerable<GameDownloadInfo>>(_downloads);
    }

    public Task<bool> IsAnyDownloadOrInstallActiveAsync()
    {
        return Task.FromResult(_isActive);
    }

    public Task<bool> InitializeAsync()
    {
        _initialized = true;
        return Task.FromResult(true);
    }

    public void SetDownloads(List<GameDownloadInfo> downloads)
    {
        _downloads.Clear();
        _downloads.AddRange(downloads);
    }

    public void SetActive(bool isActive)
    {
        _isActive = isActive;
    }

    public bool IsInitialized => _initialized;
}
