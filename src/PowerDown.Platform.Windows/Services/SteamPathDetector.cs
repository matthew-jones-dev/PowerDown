using System;
using System.IO;
using Microsoft.Win32;
using PowerDown.Core;

namespace PowerDown.Platform.Windows.Services;

public interface ISteamPathDetector
{
    string? DetectSteamPath(string? customPath);
}

public class SteamPathDetector : ISteamPathDetector
{
    private readonly ConsoleLogger _logger;

    public SteamPathDetector(ConsoleLogger logger)
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

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            
            if (key != null)
            {
                var registryPath = key.GetValue("SteamPath") as string;
                
                if (!string.IsNullOrWhiteSpace(registryPath) && Directory.Exists(registryPath))
                {
                    return Path.GetFullPath(registryPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to read Steam path from registry: {ex.Message}");
        }

        var defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Steam");

        if (Directory.Exists(defaultPath))
        {
            return defaultPath;
        }

        return null;
    }
}
