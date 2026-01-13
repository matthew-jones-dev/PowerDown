using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PowerDown.Abstractions;
using PowerDown.Core;

namespace PowerDown.Platform.Linux.Detectors;

public class LinuxEpicDownloadDetector : IDownloadDetector
{
    private readonly string? _epicPath;
    private readonly string _manifestsPath;
    private readonly ConsoleLogger _logger;
    private readonly Dictionary<string, GameDownloadInfo> _activeDownloads = new();

    public string LauncherName => "Epic Games";

    public LinuxEpicDownloadDetector(string? epicPath, ConsoleLogger logger)
    {
        _epicPath = epicPath;
        _logger = logger;
        
        if (string.IsNullOrWhiteSpace(epicPath))
        {
            throw new InvalidOperationException("Epic path is not configured");
        }

        _manifestsPath = Path.Combine(epicPath, "Manifests");
    }

    public Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_epicPath))
        {
            throw new DirectoryNotFoundException($"Epic Games directory not found: {_epicPath}");
        }

        if (!Directory.Exists(_manifestsPath))
        {
            _logger.LogWarning($"Epic manifest directory not found: {_manifestsPath}");
        }

        return Task.FromResult(true);
    }

    public async Task<IEnumerable<GameDownloadInfo>> GetActiveDownloadsAsync(CancellationToken cancellationToken = default)
    {
        await ScanManifestsAsync(cancellationToken);
        
        return _activeDownloads.Values.ToList();
    }

    public async Task<bool> IsAnyDownloadOrInstallActiveAsync(CancellationToken cancellationToken = default)
    {
        var downloads = await GetActiveDownloadsAsync(cancellationToken);
        return downloads.Any(d => 
            d.DownloadStatus == DownloadStatus.Downloading || 
            d.InstallStatus == DownloadStatus.Installing);
    }

    private async Task ScanManifestsAsync(CancellationToken cancellationToken)
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
                cancellationToken.ThrowIfCancellationRequested();
                await ParseManifestAsync(manifestFile, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error scanning Epic manifests: {ex.Message}");
        }
    }

    private async Task ParseManifestAsync(string manifestFile, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var json = await File.ReadAllTextAsync(manifestFile, cancellationToken);
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
            _logger.LogWarning($"Error parsing manifest {manifestFile}: {ex.Message}");
        }
    }
}
