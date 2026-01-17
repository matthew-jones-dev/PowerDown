using System;
using System.Collections.Generic;
using System.Linq;
using PowerDown.Abstractions;
using PowerDown.Abstractions.Interfaces;
using PowerDown.Platform.Linux.Detectors;

namespace PowerDown.Platform.Linux;

public class LinuxDetectorFactory : IDetectorFactory
{
    private readonly ISteamPathDetector _steamPathDetector;

    public LinuxDetectorFactory(ISteamPathDetector steamPathDetector)
    {
        _steamPathDetector = steamPathDetector ?? throw new ArgumentNullException(nameof(steamPathDetector));
    }

    public List<IDownloadDetector> CreateDetectors(Configuration config, ILogger logger)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        var detectors = new List<IDownloadDetector>();

        if (config.MonitorSteam)
        {
            var steamPath = _steamPathDetector.DetectSteamPath(config.CustomSteamPath);
            if (steamPath != null)
            {
                var detector = new LinuxSteamDownloadDetector(steamPath, logger);
                detectors.Add(detector);
                logger.LogInfo($"Steam detected at: {steamPath}");
            }
            else
            {
                logger.LogWarning("Steam path not found - Steam monitoring disabled");
            }
        }

        if (!detectors.Any())
        {
            logger.LogWarning("No launchers found. System will exit after checking for downloads.");
        }

        return detectors;
    }
}
