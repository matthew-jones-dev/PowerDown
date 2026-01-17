using System;
using System.IO;
using PowerDown.Abstractions.Interfaces;

namespace PowerDown.Platform.macOS.Services;

public class MacSteamPathDetector : ISteamPathDetector
{
    private readonly ILogger _logger;

    public MacSteamPathDetector(ILogger logger)
    {
        _logger = logger;
    }

    public string? DetectSteamPath(string? customPath)
    {
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            if (Directory.Exists(customPath))
            {
                return Path.GetFullPath(customPath);
            }
            
            _logger.LogWarning($"Custom Steam path does not exist: {customPath}");
            return null;
        }

        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var steamPath = Path.Combine(homeDir, "Library", "Application Support", "Steam");

        if (Directory.Exists(steamPath))
        {
            return steamPath;
        }

        _logger.LogWarning("Steam installation not found on macOS");
        return null;
    }
}
