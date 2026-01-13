using System;
using System.IO;
using PowerDown.Abstractions;
using PowerDown.Core;

namespace PowerDown.Platform.macOS.Services;

public class MacPathDetector
{
    private readonly ConsoleLogger _logger;

    public MacPathDetector(ConsoleLogger logger)
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

    public string? DetectEpicPath(string? customPath)
    {
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            if (Directory.Exists(customPath))
            {
                return Path.GetFullPath(customPath);
            }
            
            _logger.LogWarning($"Custom Epic path does not exist: {customPath}");
            return null;
        }

        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var epicPath = Path.Combine(homeDir, "Library", "Application Support", "Epic");

        if (Directory.Exists(epicPath))
        {
            return epicPath;
        }

        var gamesPath = Path.Combine(homeDir, "Games", "Epic");
        if (Directory.Exists(gamesPath))
        {
            return gamesPath;
        }

        _logger.LogWarning("Epic Games installation not found on macOS");
        return null;
    }
}
