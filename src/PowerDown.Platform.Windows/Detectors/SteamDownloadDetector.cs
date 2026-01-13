using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PowerDown.Abstractions;
using PowerDown.Core;
using PowerDown.Core.Detectors;

namespace PowerDown.Platform.Windows.Detectors;

public class SteamDownloadDetector : SteamDownloadDetectorBase
{
    private readonly string _contentLogPath;

    protected override string ContentLogPath => _contentLogPath;

    protected override string LineSeparator => Environment.NewLine;

    public SteamDownloadDetector(string? steamPath, ConsoleLogger logger)
        : base(steamPath, logger)
    {
        if (string.IsNullOrWhiteSpace(steamPath))
        {
            throw new InvalidOperationException("Steam path is not configured");
        }

        _contentLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Steam",
            "logs",
            "content_log.txt");
    }

    public override async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(Path.GetDirectoryName(_contentLogPath)))
        {
            throw new DirectoryNotFoundException($"Steam directory not found");
        }

        if (!File.Exists(_contentLogPath))
        {
            Logger.LogWarning($"Steam content_log.txt not found: {_contentLogPath}");
        }
        else
        {
            await ParseContentLogAsync(true, cancellationToken);
        }

        await ScanAppManifestsAsync(cancellationToken);
        
        return true;
    }

    private ConsoleLogger Logger => (ConsoleLogger)typeof(SteamDownloadDetectorBase)
        .GetField("_logger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
        .GetValue(this)!;
}
