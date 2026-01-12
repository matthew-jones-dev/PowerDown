using System;
using System.IO;
using System.Text.Json;

namespace PowerDown.Platform.Windows.Services;

public class EpicPathDetector
{
    public static string? DetectEpicPath(string? customPath)
    {
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            if (Directory.Exists(customPath))
            {
                return Path.GetFullPath(customPath);
            }
            
            Console.WriteLine($"[WARN] Custom Epic path does not exist: {customPath}");
            return null;
        }

        var launcherInstalledPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Epic",
            "UnrealEngineLauncher",
            "LauncherInstalled.dat");

        try
        {
            if (File.Exists(launcherInstalledPath))
            {
                var json = File.ReadAllText(launcherInstalledPath);
                var root = JsonSerializer.Deserialize<EpicLauncherRoot>(json);
                
                if (root?.InstallationList != null && root.InstallationList.Length > 0)
                {
                    var firstGame = root.InstallationList[0];
                    var gamePath = firstGame.InstallLocation;
                    
                    var epicPath = Path.GetDirectoryName(gamePath);
                    
                    if (!string.IsNullOrWhiteSpace(epicPath) && Directory.Exists(epicPath))
                    {
                        return Path.GetFullPath(epicPath);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Failed to parse LauncherInstalled.dat: {ex.Message}");
        }

        var defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Epic Games");

        if (Directory.Exists(defaultPath))
        {
            return defaultPath;
        }

        return null;
    }

    private class EpicLauncherRoot
    {
        public EpicInstallation[]? InstallationList { get; set; }
    }

    private class EpicInstallation
    {
        public string? InstallLocation { get; set; }
        public string? AppName { get; set; }
    }
}
