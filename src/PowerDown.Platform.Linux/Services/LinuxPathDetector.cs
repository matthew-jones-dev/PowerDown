using System;
using System.IO;
using PowerDown.Abstractions;
using PowerDown.Core;

namespace PowerDown.Platform.Linux.Services;

public interface ILinuxPathDetector
{
    string? DetectSteamPath(string? customPath);
    string? DetectEpicPath(string? customPath);
}

public class LinuxPathDetector : ILinuxPathDetector
{
    private readonly ConsoleLogger _logger;

    public LinuxPathDetector(ConsoleLogger logger)
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
        var steamPath = Path.Combine(homeDir, ".local", "share", "Steam");

        if (Directory.Exists(steamPath))
        {
            return steamPath;
        }

        var protonPath = Path.Combine(homeDir, ".steam", "steam");
        if (Directory.Exists(protonPath))
        {
            return protonPath;
        }

        _logger.LogWarning("Steam installation not found on Linux");
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
        var epicPath = Path.Combine(homeDir, ".local", "share", "Epic");

        if (Directory.Exists(epicPath))
        {
            return epicPath;
        }

        var gamesPath = Path.Combine(homeDir, "Games", "Epic");
        if (Directory.Exists(gamesPath))
        {
            return gamesPath;
        }

        _logger.LogWarning("Epic Games installation not found on Linux");
        return null;
    }
}
