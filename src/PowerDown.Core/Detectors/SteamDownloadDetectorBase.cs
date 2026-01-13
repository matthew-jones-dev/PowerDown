using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PowerDown.Abstractions;
using PowerDown.Core.Services;

namespace PowerDown.Core.Detectors;

/// <summary>
/// Base class for Steam download detection across all platforms.
/// Contains all common detection logic, with platform-specific path resolution delegated to subclasses.
/// </summary>
public abstract class SteamDownloadDetectorBase : IDownloadDetector
{
    private readonly string? _steamPath;
    private readonly string _downloadingPath;
    private readonly string _steamAppsPath;
    private readonly ConsoleLogger _logger;
    private long _lastLogPosition = 0;
    private readonly Dictionary<string, GameDownloadInfo> _activeDownloads = new();

    /// <summary>
    /// Gets the platform-specific path to the Steam content log file.
    /// </summary>
    protected abstract string ContentLogPath { get; }

    /// <summary>
    /// Gets the platform-specific line separator used in the content log.
    /// </summary>
    protected abstract string LineSeparator { get; }

    /// <inheritdoc />
    public string LauncherName => "Steam";

    /// <summary>
    /// Creates a new SteamDownloadDetectorBase.
    /// </summary>
    /// <param name="steamPath">The path to the Steam installation directory.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when steamPath is null or whitespace.</exception>
    protected SteamDownloadDetectorBase(string? steamPath, ConsoleLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        if (string.IsNullOrWhiteSpace(steamPath))
        {
            throw new InvalidOperationException("Steam path is not configured");
        }
        
        _steamPath = steamPath;
        _downloadingPath = Path.Combine(steamPath, "steamapps", "downloading");
        _steamAppsPath = Path.Combine(steamPath, "steamapps");
    }

    /// <inheritdoc />
    public virtual Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_steamPath))
        {
            throw new DirectoryNotFoundException($"Steam directory not found: {_steamPath}");
        }

        if (!File.Exists(ContentLogPath))
        {
            _logger.LogWarning($"Steam content_log.txt not found: {ContentLogPath}");
        }

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GameDownloadInfo>> GetActiveDownloadsAsync(CancellationToken cancellationToken = default)
    {
        await ParseContentLogAsync(false, cancellationToken);
        await ScanAppManifestsAsync(cancellationToken);
        
        return _activeDownloads.Values.ToList();
    }

    /// <inheritdoc />
    public async Task<bool> IsAnyDownloadOrInstallActiveAsync(CancellationToken cancellationToken = default)
    {
        var downloads = await GetActiveDownloadsAsync(cancellationToken);
        return downloads.Any(d => 
            d.DownloadStatus == DownloadStatus.Downloading || 
            d.InstallStatus == DownloadStatus.Installing);
    }

    /// <summary>
    /// Parses the Steam content log file for download activity.
    /// </summary>
    /// <param name="isInitial">Whether this is the initial parse (seeks from beginning).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected async Task ParseContentLogAsync(bool isInitial, CancellationToken cancellationToken)
    {
        if (!File.Exists(ContentLogPath))
        {
            return;
        }

        try
        {
            using var reader = new StreamReader(ContentLogPath);
            
            if (!isInitial)
            {
                reader.BaseStream.Seek(_lastLogPosition, SeekOrigin.Begin);
            }

            var content = await reader.ReadToEndAsync();
            _lastLogPosition = reader.BaseStream.Position;

            var lines = content.Split(new[] { LineSeparator }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                ParseLogLine(line);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error parsing Steam content log: {ex.Message}");
        }
    }

    /// <summary>
    /// Parses a single line from the Steam content log.
    /// </summary>
    /// <param name="line">The log line to parse.</param>
    protected void ParseLogLine(string line)
    {
        var downloadingMatch = DownloadProgressRegex.Match(line);
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

        var completeMatch = DownloadCompleteRegex.Match(line);
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

        var installedMatch = InstallationCompleteRegex.Match(line);
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
                _logger.LogWarning($"Error scanning downloading folder: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Extracts the game name from a log line.
    /// </summary>
    /// <param name="line">The log line.</param>
    /// <returns>The extracted game name or "Unknown Game".</returns>
    protected string ExtractGameName(string line)
    {
        var match = StartingGameRegex.Match(line);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        match = ForGameRegex.Match(line);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        return "Unknown Game";
    }

    /// <summary>
    /// Scans Steam app manifests for installed games and their states.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected async Task ScanAppManifestsAsync(CancellationToken cancellationToken)
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
                cancellationToken.ThrowIfCancellationRequested();
                await ParseAppManifestAsync(manifestFile, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error scanning Steam app manifests: {ex.Message}");
        }
    }

    /// <summary>
    /// Parses a Steam app manifest file to extract game state.
    /// </summary>
    /// <param name="manifestFile">The path to the manifest file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected async Task ParseAppManifestAsync(string manifestFile, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var lines = await File.ReadAllLinesAsync(manifestFile, cancellationToken);
            
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

            var (downloadStatus, installStatus, progress) = SteamStateFlags.Interpret(stateFlags.Value);

            if (stateFlags.Value == SteamStateFlags.FullyInstalled)
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
            _logger.LogWarning($"Error parsing app manifest {manifestFile}: {ex.Message}");
        }
    }

    /// <summary>
    /// Ensures a GameDownloadInfo entry exists for the given game name.
    /// </summary>
    /// <param name="gameName">The game name.</param>
    /// <returns>The existing or newly created GameDownloadInfo.</returns>
    protected GameDownloadInfo EnsureGameInfo(string gameName)
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

    #region Pre-compiled Regex Patterns

    private static readonly Regex DownloadProgressRegex = new(
        @"Downloading\s+([\d.]+)\s+GiB",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex DownloadCompleteRegex = new(
        @"Download complete|Download finished",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex InstallationCompleteRegex = new(
        @"Installed|Installation complete",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex StartingGameRegex = new(
        @"Starting\s+([^\[\]]+)",
        RegexOptions.Compiled);

    private static readonly Regex ForGameRegex = new(
        @"for\s+([^\[\]]+)",
        RegexOptions.Compiled);

    #endregion
}

/// <summary>
/// Steam client state flags as defined in Steam's state machine.
/// Retrieved from Steam's public headers and reverse-engineered behavior.
/// </summary>
public static class SteamStateFlags
{
    /// <summary>
    /// State 4: Fully installed and idle.
    /// Game is installed, not downloading, not installing updates.
    /// Display: Idle/Idle, Progress: 100%
    /// </summary>
    public const int FullyInstalled = 4;
    
    /// <summary>
    /// State 6: Installing update.
    /// Game has update in progress, download complete.
    /// Display: Idle/Installing, Progress: 95%
    /// </summary>
    public const int InstallingUpdate = 6;
    
    /// <summary>
    /// State 1026: Actively downloading.
    /// Game is downloading content.
    /// Display: Downloading/Unknown, Progress: 50%
    /// </summary>
    public const int Downloading = 1026;
    
    /// <summary>
    /// Interprets state flags into human-readable status.
    /// </summary>
    /// <param name="flags">The state flags value from the manifest.</param>
    /// <returns>A tuple of (downloadStatus, installStatus, progress).</returns>
    public static (DownloadStatus download, DownloadStatus install, double progress) 
        Interpret(int flags) => flags switch
    {
        FullyInstalled => (DownloadStatus.Idle, DownloadStatus.Idle, 100.0),
        InstallingUpdate => (DownloadStatus.Idle, DownloadStatus.Installing, 95.0),
        Downloading => (DownloadStatus.Downloading, DownloadStatus.Unknown, 50.0),
        _ => (DownloadStatus.Unknown, DownloadStatus.Unknown, 0.0)
    };
}
