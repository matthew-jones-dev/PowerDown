using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PowerDown.Abstractions;

namespace PowerDown.Platform.Windows.Detectors;

public class SteamDownloadDetector : IDownloadDetector
{
    private readonly string? _steamPath;
    private readonly string _contentLogPath;
    private readonly string _downloadingPath;
    private readonly string _steamAppsPath;
    private long _lastLogPosition = 0;
    private readonly Dictionary<string, GameDownloadInfo> _activeDownloads = new();

    public string LauncherName => "Steam";

    public SteamDownloadDetector(string? steamPath)
    {
        _steamPath = steamPath;
        
        if (string.IsNullOrWhiteSpace(steamPath))
        {
            throw new InvalidOperationException("Steam path is not configured");
        }

        _contentLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Steam",
            "logs",
            "content_log.txt");

        _downloadingPath = Path.Combine(steamPath, "steamapps", "downloading");
        _steamAppsPath = Path.Combine(steamPath, "steamapps");
    }

    public async Task<bool> InitializeAsync()
    {
        if (!Directory.Exists(_steamPath))
        {
            throw new DirectoryNotFoundException($"Steam directory not found: {_steamPath}");
        }

        if (!File.Exists(_contentLogPath))
        {
            Console.WriteLine($"[WARN] Steam content_log.txt not found: {_contentLogPath}");
        }
        else
        {
            await ParseContentLogAsync(true);
        }

        await ScanAppManifestsAsync();
        
        return true;
    }

    public async Task<IEnumerable<GameDownloadInfo>> GetActiveDownloadsAsync()
    {
        await ParseContentLogAsync(false);
        await ScanAppManifestsAsync();
        
        return _activeDownloads.Values.ToList();
    }

    public async Task<bool> IsAnyDownloadOrInstallActiveAsync()
    {
        var downloads = await GetActiveDownloadsAsync();
        return downloads.Any(d => 
            d.DownloadStatus == DownloadStatus.Downloading || 
            d.InstallStatus == DownloadStatus.Installing);
    }

    private async Task ParseContentLogAsync(bool isInitial = false)
    {
        if (!File.Exists(_contentLogPath))
        {
            return;
        }

        try
        {
            using var reader = new StreamReader(_contentLogPath);
            
            if (!isInitial)
            {
                reader.BaseStream.Seek(_lastLogPosition, SeekOrigin.Begin);
            }

            var content = await reader.ReadToEndAsync();
            _lastLogPosition = reader.BaseStream.Position;

            var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                ParseLogLine(line);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Error parsing Steam content log: {ex.Message}");
        }
    }

    private void ParseLogLine(string line)
    {
        var downloadingMatch = Regex.Match(line, @"Downloading\s+([\d.]+)\s+GiB");
        if (downloadingMatch.Success)
        {
            var gameName = ExtractGameName(line);
            if (!string.IsNullOrWhiteSpace(gameName))
            {
                EnsureGameInfo(gameName).DownloadStatus = DownloadStatus.Downloading;
                EnsureGameInfo(gameName).Progress = 0;
            }
            return;
        }

        var completeMatch = Regex.Match(line, @"Download complete|Download finished");
        if (completeMatch.Success)
        {
            var gameName = ExtractGameName(line);
            if (!string.IsNullOrWhiteSpace(gameName))
            {
                var info = EnsureGameInfo(gameName);
                info.DownloadStatus = DownloadStatus.Idle;
                info.Progress = 100;
            }
            return;
        }

        var installedMatch = Regex.Match(line, @"Installed|Installation complete");
        if (installedMatch.Success)
        {
            var gameName = ExtractGameName(line);
            if (!string.IsNullOrWhiteSpace(gameName))
            {
                var info = EnsureGameInfo(gameName);
                info.InstallStatus = DownloadStatus.Idle;
                info.Progress = 100;
            }
            return;
        }

        if (Directory.Exists(_downloadingPath))
        {
            try
            {
                var downloadingDirs = Directory.GetDirectories(_downloadingPath);
                foreach (var dir in downloadingDirs)
                {
                    var dirName = Path.GetFileName(dir);
                    if (!_activeDownloads.ContainsKey(dirName))
                    {
                        var info = new GameDownloadInfo
                        {
                            GameName = dirName,
                            LauncherName = LauncherName,
                            DownloadStatus = DownloadStatus.Downloading,
                            InstallStatus = DownloadStatus.Unknown,
                            Progress = 0
                        };
                        _activeDownloads[dirName] = info;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Error scanning downloading folder: {ex.Message}");
            }
        }
    }

    private string ExtractGameName(string line)
    {
        var match = Regex.Match(line, @"Starting\s+([^\[\]]+)");
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        match = Regex.Match(line, @"for\s+([^\[\]]+)");
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        return "Unknown Game";
    }

    private async Task ScanAppManifestsAsync()
    {
        if (!Directory.Exists(_steamAppsPath))
        {
            return;
        }

        try
        {
            var manifestFiles = Directory.GetFiles(_steamAppsPath, "appmanifest_*.acf");
            
            foreach (var manifestFile in manifestFiles)
            {
                await ParseAppManifestAsync(manifestFile);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Error scanning Steam app manifests: {ex.Message}");
        }
    }

    private async Task ParseAppManifestAsync(string manifestFile)
    {
        try
        {
            var lines = await File.ReadAllLinesAsync(manifestFile);
            
            string? gameName = null;
            int? stateFlags = null;
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                if (trimmed.Contains("\"name\""))
                {
                    var parts = trimmed.Split('"');
                    if (parts.Length >= 2)
                    {
                        var key = parts[1];
                        if (parts.Length >= 4)
                        {
                            gameName = parts[3];
                        }
                    }
                }
                else if (trimmed.Contains("\"StateFlags\""))
                {
                    var parts = trimmed.Split('"');
                    if (parts.Length >= 2 && int.TryParse(parts[3], out var flags))
                    {
                        stateFlags = flags;
                    }
                }
            }
            
            if (gameName == null || stateFlags == null)
            {
                return;
            }

            var downloadStatus = DownloadStatus.Idle;
            var installStatus = DownloadStatus.Idle;
            var progress = 100.0;

            switch (stateFlags.Value)
            {
                case 4:
                    downloadStatus = DownloadStatus.Idle;
                    installStatus = DownloadStatus.Idle;
                    progress = 100.0;
                    break;
                case 6:
                    downloadStatus = DownloadStatus.Idle;
                    installStatus = DownloadStatus.Installing;
                    progress = 95.0;
                    break;
                case 1026:
                    downloadStatus = DownloadStatus.Downloading;
                    installStatus = DownloadStatus.Unknown;
                    progress = 50.0;
                    break;
            }

            if (stateFlags.Value == 4)
            {
                _activeDownloads.Remove(gameName);
            }
            else if (_activeDownloads.ContainsKey(gameName))
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
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Error parsing app manifest {manifestFile}: {ex.Message}");
        }
    }

    private GameDownloadInfo EnsureGameInfo(string gameName)
    {
        if (!_activeDownloads.ContainsKey(gameName))
        {
            _activeDownloads[gameName] = new GameDownloadInfo
            {
                GameName = gameName,
                LauncherName = LauncherName,
                DownloadStatus = DownloadStatus.Unknown,
                InstallStatus = DownloadStatus.Unknown,
                Progress = 0
            };
        }
        return _activeDownloads[gameName];
    }
}
