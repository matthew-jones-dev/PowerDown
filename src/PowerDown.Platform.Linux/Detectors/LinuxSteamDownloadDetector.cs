using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PowerDown.Abstractions;
using PowerDown.Abstractions.Interfaces;
using PowerDown.Core;
using PowerDown.Core.Detectors;

namespace PowerDown.Platform.Linux.Detectors;

public class LinuxSteamDownloadDetector : SteamDownloadDetectorBase
{
    private readonly string _contentLogPath;

    protected override string ContentLogPath => _contentLogPath;

    protected override string LineSeparator => "\n";

    public LinuxSteamDownloadDetector(string? steamPath, ILogger logger)
        : base(steamPath, logger)
    {
        if (string.IsNullOrWhiteSpace(steamPath))
        {
            throw new InvalidOperationException("Steam path is not configured");
        }

        _contentLogPath = Path.Combine(steamPath, "logs", "content_log.txt");
    }
}
