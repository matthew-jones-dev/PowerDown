using System;
using System.IO;
using Microsoft.Win32;

namespace PowerDown.Platform.Windows.Services;

public class SteamPathDetector
{
    public static string? DetectSteamPath(string? customPath)
    {
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            if (Directory.Exists(customPath))
            {
                return Path.GetFullPath(customPath);
            }
            
            Console.WriteLine($"[WARN] Custom Steam path does not exist: {customPath}");
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
            Console.WriteLine($"[WARN] Failed to read Steam path from registry: {ex.Message}");
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
