using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PowerDown.Abstractions;

public interface IDownloadDetector
{
    string LauncherName { get; }
    Task<IEnumerable<GameDownloadInfo>> GetActiveDownloadsAsync(CancellationToken cancellationToken = default);
    Task<bool> IsAnyDownloadOrInstallActiveAsync(CancellationToken cancellationToken = default);
    Task<bool> InitializeAsync(CancellationToken cancellationToken = default);
}
