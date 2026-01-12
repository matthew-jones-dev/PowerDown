using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PowerDown.Abstractions;

namespace PowerDown.Platform.Windows.Detectors;

public class EpicDownloadDetector : IDownloadDetector
{
    private readonly string? _epicPath;
    private readonly string _manifestsPath;
    private readonly string _launcherInstalledPath;
    private readonly Dictionary<string, GameDownloadInfo> _activeDownloads = new();

    public string LauncherName => "Epic Games";

    public EpicDownloadDetector(string? epicPath)
    {
        _epicPath = epicPath;
        
        if (string.IsNullOrWhiteSpace(epicPath))
        {
            throw new InvalidOperationException("Epic path is not configured");
        }

        _manifestsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Epic",
            "EpicGamesLauncher",
            "Data",
            "Manifests");

        _launcherInstalledPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Epic",
            "UnrealEngineLauncher",
            "LauncherInstalled.dat");
    }

    public async Task<bool> InitializeAsync()
    {
        if (!Directory.Exists(_epicPath))
        {
            throw new DirectoryNotFoundException($"Epic Games directory not found: {_epicPath}");
        }

        await ParseLauncherInstalledAsync();
        
        if (!Directory.Exists(_manifestsPath))
        {
            Console.WriteLine($"[WARN] Epic manifest directory not found: {_manifestsPath}");
        }
        else
        {
            await ScanManifestsAsync();
        }

        return true;
    }

    public async Task<IEnumerable<GameDownloadInfo>> GetActiveDownloadsAsync()
    {
        await ScanManifestsAsync();
        
        return _activeDownloads.Values.ToList();
    }

    public async Task<bool> IsAnyDownloadOrInstallActiveAsync()
    {
        var downloads = await GetActiveDownloadsAsync();
        return downloads.Any(d => 
            d.DownloadStatus == DownloadStatus.Downloading || 
            d.InstallStatus == DownloadStatus.Installing);
    }

    private async Task ParseLauncherInstalledAsync()
    {
        if (!File.Exists(_launcherInstalledPath))
        {
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_launcherInstalledPath);
            var root = JsonSerializer.Deserialize<EpicLauncherRoot>(json);
            
            if (root?.InstallationList != null)
            {
                foreach (var installation in root.InstallationList)
                {
                    if (!string.IsNullOrWhiteSpace(installation.InstallLocation) && 
                        !string.IsNullOrWhiteSpace(installation.AppName))
                    {
                        var gameName = installation.AppName;
                        
                        if (!_activeDownloads.ContainsKey(gameName))
                        {
                            _activeDownloads[gameName] = new GameDownloadInfo
                            {
                                GameName = gameName,
                                LauncherName = LauncherName,
                                DownloadStatus = DownloadStatus.Idle,
                                InstallStatus = DownloadStatus.Idle,
                                Progress = 100.0
                            };
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Error parsing LauncherInstalled.dat: {ex.Message}");
        }
    }

    private async Task ScanManifestsAsync()
    {
        if (!Directory.Exists(_manifestsPath))
        {
            return;
        }

        try
        {
            var manifestFiles = Directory.GetFiles(_manifestsPath, "*.manifest");
            
            foreach (var manifestFile in manifestFiles)
            {
                await ParseManifestAsync(manifestFile);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Error scanning Epic manifests: {ex.Message}");
        }
    }

    private async Task ParseManifestAsync(string manifestFile)
    {
        try
        {
            var json = await File.ReadAllTextAsync(manifestFile);
            var root = JsonSerializer.Deserialize<JsonElement>(json);
            
            if (root.ValueKind != JsonValueKind.Undefined &&
                root.TryGetProperty("Manifest", out var actualManifest) &&
                actualManifest.ValueKind != JsonValueKind.Undefined &&
                actualManifest.TryGetProperty("AppName", out var appNameProp) &&
                actualManifest.TryGetProperty("AppVersion", out var appVersionProp))
            {
                var gameName = appNameProp.GetString() ?? "Unknown Game";
                var appVersion = appVersionProp.GetString() ?? "";
                
                var downloadStatus = DownloadStatus.Idle;
                var installStatus = DownloadStatus.Idle;
                var progress = 100.0;

                if (appVersion.Contains("+Downloading"))
                {
                    downloadStatus = DownloadStatus.Downloading;
                    installStatus = DownloadStatus.Unknown;
                    progress = 50.0;
                }
                else if (appVersion.Contains("+Installing"))
                {
                    downloadStatus = DownloadStatus.Idle;
                    installStatus = DownloadStatus.Installing;
                    progress = 95.0;
                }
                else
                {
                    _activeDownloads.Remove(gameName);
                    return;
                }

                if (_activeDownloads.ContainsKey(gameName))
                {
                    _activeDownloads[gameName].DownloadStatus = downloadStatus;
                    _activeDownloads[gameName].InstallStatus = installStatus;
                    _activeDownloads[gameName].Progress = progress;
                }
                else
                {
                    var info = new GameDownloadInfo
                    {
                        GameName = gameName,
                        LauncherName = LauncherName,
                        DownloadStatus = downloadStatus,
                        InstallStatus = installStatus,
                        Progress = progress
                    };
                    _activeDownloads[gameName] = info;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Error parsing manifest {manifestFile}: {ex.Message}");
        }
    }

    private class EpicLauncherRoot
    {
        public EpicInstallation[]? InstallationList { get; set; }
    }

    private class EpicInstallation
    {
        public string? InstallLocation { get; set; }
        public string? AppName { get; set; }
        public string? AppVersion { get; set; }
    }
}
