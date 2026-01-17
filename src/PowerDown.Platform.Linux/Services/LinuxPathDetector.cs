using System;
using System.IO;
using PowerDown.Abstractions.Interfaces;

namespace PowerDown.Platform.Linux.Services;

public class LinuxSteamPathDetector : ISteamPathDetector
{
    private readonly ILogger _logger;

    public LinuxSteamPathDetector(ILogger logger)
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
        var candidates = new[]
        {
            Path.Combine(homeDir, ".local", "share", "Steam"),
            Path.Combine(homeDir, ".steam", "steam"),
            Path.Combine(homeDir, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam"),
            Path.Combine(homeDir, "snap", "steam", "common", ".steam", "steam")
        };

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        _logger.LogWarning("Steam installation not found on Linux");
        return null;
    }
}
