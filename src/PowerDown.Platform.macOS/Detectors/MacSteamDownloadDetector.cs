using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PowerDown.Abstractions;
using PowerDown.Core;
using PowerDown.Core.Detectors;

namespace PowerDown.Platform.macOS.Detectors;

public class MacSteamDownloadDetector : SteamDownloadDetectorBase
{
    private readonly string _contentLogPath;

    protected override string ContentLogPath => _contentLogPath;

    protected override string LineSeparator => "\n";

    public MacSteamDownloadDetector(string? steamPath, ConsoleLogger logger)
        : base(steamPath, logger)
    {
        if (string.IsNullOrWhiteSpace(steamPath))
        {
            throw new InvalidOperationException("Steam path is not configured");
        }

        _contentLogPath = Path.Combine(steamPath, "logs", "content_log.txt");
    }
}
