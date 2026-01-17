using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PowerDown.Abstractions;
using PowerDown.Abstractions.Interfaces;
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
    private readonly List<string> _steamAppsPaths = new();
    private readonly List<string> _downloadingPaths = new();
    private readonly ILogger _logger;
    private long _lastLogPosition = 0;
    private readonly Dictionary<string, GameDownloadInfo> _activeDownloads = new();
    private readonly Dictionary<string, string> _appIdToName = new();

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
    protected SteamDownloadDetectorBase(string? steamPath, ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        if (string.IsNullOrWhiteSpace(steamPath))
        {
            throw new InvalidOperationException("Steam path is not configured");
        }
        
        _steamPath = steamPath;
        _downloadingPath = Path.Combine(steamPath, "steamapps", "downloading");
        _steamAppsPath = Path.Combine(steamPath, "steamapps");
        _steamAppsPaths.Add(_steamAppsPath);
        _downloadingPaths.Add(_downloadingPath);
    }

    /// <inheritdoc />
    public virtual Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_steamPath))
        {
            throw new DirectoryNotFoundException($"Steam directory not found: {_steamPath}");
        }

        RefreshLibraryFolders();

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
            using var stream = new FileStream(ContentLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            
            if (!isInitial)
            {
                reader.BaseStream.Seek(_lastLogPosition, SeekOrigin.Begin);
            }

            var content = await reader.ReadToEndAsync();
            _lastLogPosition = reader.BaseStream.Position;

            var lines = Regex.Split(content, "\r?\n");

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
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        var updateChangedMatch = AppIdUpdateChangedRegex.Match(line);
        if (updateChangedMatch.Success)
        {
            var appId = updateChangedMatch.Groups["id"].Value;
            var state = updateChangedMatch.Groups["state"].Value;
            UpdateDownloadFromState(appId, state);
            return;
        }

        var updateStartedMatch = AppIdUpdateStartedRegex.Match(line);
        if (updateStartedMatch.Success)
        {
            var appId = updateStartedMatch.Groups["id"].Value;
            if (!_appIdToName.ContainsKey(appId))
            {
                return;
            }
            var info = EnsureGameInfoForAppId(appId);
            info.DownloadStatus = DownloadStatus.Downloading;
            info.InstallStatus = DownloadStatus.Unknown;
            info.Progress = 0;
            return;
        }

        var stateChangedMatch = AppIdStateChangedRegex.Match(line);
        if (stateChangedMatch.Success)
        {
            var appId = stateChangedMatch.Groups["id"].Value;
            var state = stateChangedMatch.Groups["state"].Value;
            if (IsFullyInstalledState(state))
            {
                MarkIdle(appId);
                return;
            }
        }

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

        foreach (var downloadingPath in _downloadingPaths)
        {
            if (!Directory.Exists(downloadingPath))
            {
                continue;
            }

            try
            {
                var downloadingDirs = Directory.GetDirectories(downloadingPath);
                foreach (var dir in downloadingDirs)
                {
                    var dirName = Path.GetFileName(dir);
                    if (!_activeDownloads.ContainsKey(dirName))
                    {
                        var info = new GameDownloadInfo
                        {
                            GameName = _appIdToName.TryGetValue(dirName, out var name) ? name : dirName,
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

        return string.Empty;
    }

    /// <summary>
    /// Scans Steam app manifests for installed games and their states.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected async Task ScanAppManifestsAsync(CancellationToken cancellationToken)
    {
        RefreshLibraryFolders();

        try
        {
            foreach (var steamAppsPath in _steamAppsPaths)
            {
                if (!Directory.Exists(steamAppsPath))
                {
                    continue;
                }

                var manifestFiles = Directory.GetFiles(steamAppsPath, "appmanifest_*.acf");

                foreach (var manifestFile in manifestFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await ParseAppManifestAsync(manifestFile, cancellationToken);
                }
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
            
            var appId = ExtractAppIdFromFileName(manifestFile);
            string? gameName = null;
            int? stateFlags = null;
            long? bytesToDownload = null;
            long? bytesDownloaded = null;
            long? bytesToStage = null;
            long? bytesStaged = null;
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                if (trimmed.Contains("\"appid\""))
                {
                    var parts = trimmed.Split('"');
                    if (parts.Length >= 4)
                    {
                        appId = parts[3];
                    }
                }
                else if (trimmed.Contains("\"name\""))
                {
                    var parts = trimmed.Split('"');
                    if (parts.Length >= 2)
                    {
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
                else if (trimmed.Contains("\"BytesToDownload\""))
                {
                    bytesToDownload = TryParseLongField(trimmed);
                }
                else if (trimmed.Contains("\"BytesDownloaded\""))
                {
                    bytesDownloaded = TryParseLongField(trimmed);
                }
                else if (trimmed.Contains("\"BytesToStage\""))
                {
                    bytesToStage = TryParseLongField(trimmed);
                }
                else if (trimmed.Contains("\"BytesStaged\""))
                {
                    bytesStaged = TryParseLongField(trimmed);
                }
            }
            
            if (appId == null && gameName == null)
            {
                return;
            }

            var key = appId ?? gameName!;
            var resolvedName = gameName ?? $"AppID {key}";
            if (appId != null)
            {
                _appIdToName[appId] = resolvedName;
            }

            var (downloadStatus, installStatus, progress) = ResolveStatusFromManifest(
                stateFlags,
                bytesToDownload,
                bytesDownloaded,
                bytesToStage,
                bytesStaged);

            if (downloadStatus == DownloadStatus.Idle && installStatus == DownloadStatus.Idle)
            {
                _activeDownloads.Remove(key);
            }
            else if (_activeDownloads.ContainsKey(key))
            {
                _activeDownloads[key].DownloadStatus = downloadStatus;
                _activeDownloads[key].InstallStatus = installStatus;
                _activeDownloads[key].Progress = progress;
                _activeDownloads[key].GameName = resolvedName;
            }
            else
            {
                var info = new GameDownloadInfo
                {
                    GameName = resolvedName,
                    LauncherName = LauncherName,
                    DownloadStatus = downloadStatus,
                    InstallStatus = installStatus,
                    Progress = progress
                };
                _activeDownloads[key] = info;
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

    private GameDownloadInfo EnsureGameInfoForAppId(string appId)
    {
        if (!_activeDownloads.ContainsKey(appId))
        {
            var gameName = _appIdToName.TryGetValue(appId, out var name)
                ? name
                : $"AppID {appId}";

            _activeDownloads[appId] = new GameDownloadInfo
            {
                GameName = gameName,
                LauncherName = LauncherName,
                DownloadStatus = DownloadStatus.Unknown,
                InstallStatus = DownloadStatus.Unknown,
                Progress = 0
            };
        }
        else if (_appIdToName.TryGetValue(appId, out var resolvedName))
        {
            _activeDownloads[appId].GameName = resolvedName;
        }
        return _activeDownloads[appId];
    }

    private void UpdateDownloadFromState(string appId, string state)
    {
        if (!_appIdToName.ContainsKey(appId))
        {
            return;
        }

        if (state.Contains("Downloading", StringComparison.OrdinalIgnoreCase))
        {
            var info = EnsureGameInfoForAppId(appId);
            info.DownloadStatus = DownloadStatus.Downloading;
            info.InstallStatus = DownloadStatus.Unknown;
            info.Progress = 0;
            return;
        }

        if (IsInstallingState(state))
        {
            var info = EnsureGameInfoForAppId(appId);
            info.DownloadStatus = DownloadStatus.Idle;
            info.InstallStatus = DownloadStatus.Installing;
            info.Progress = 95;
            return;
        }

        if (state.Contains("None", StringComparison.OrdinalIgnoreCase))
        {
            MarkIdle(appId);
        }
    }

    private void MarkIdle(string appId)
    {
        if (!_appIdToName.ContainsKey(appId))
        {
            return;
        }

        if (_activeDownloads.TryGetValue(appId, out var info))
        {
            info.DownloadStatus = DownloadStatus.Idle;
            info.InstallStatus = DownloadStatus.Idle;
            info.Progress = 100;
        }
        else
        {
            _activeDownloads[appId] = new GameDownloadInfo
            {
                GameName = _appIdToName.TryGetValue(appId, out var name) ? name : $"AppID {appId}",
                LauncherName = LauncherName,
                DownloadStatus = DownloadStatus.Idle,
                InstallStatus = DownloadStatus.Idle,
                Progress = 100
            };
        }
    }

    private static bool IsInstallingState(string state)
    {
        return state.Contains("Staging", StringComparison.OrdinalIgnoreCase) ||
               state.Contains("Committing", StringComparison.OrdinalIgnoreCase) ||
               state.Contains("Preallocating", StringComparison.OrdinalIgnoreCase) ||
               state.Contains("Reconfiguring", StringComparison.OrdinalIgnoreCase) ||
               state.Contains("Validating", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFullyInstalledState(string state)
    {
        return state.Contains("Fully Installed", StringComparison.OrdinalIgnoreCase) &&
               !state.Contains("Update Running", StringComparison.OrdinalIgnoreCase) &&
               !state.Contains("Update Started", StringComparison.OrdinalIgnoreCase) &&
               !state.Contains("Update Queued", StringComparison.OrdinalIgnoreCase);
    }

    private static (DownloadStatus download, DownloadStatus install, double progress) ResolveStatusFromManifest(
        int? stateFlags,
        long? bytesToDownload,
        long? bytesDownloaded,
        long? bytesToStage,
        long? bytesStaged)
    {
        var hasDownloadBytes = bytesToDownload.HasValue && bytesDownloaded.HasValue;
        var hasStageBytes = bytesToStage.HasValue && bytesStaged.HasValue;

        var remainingDownload = hasDownloadBytes ? Math.Max(0, bytesToDownload!.Value - bytesDownloaded!.Value) : 0;
        var remainingStage = hasStageBytes ? Math.Max(0, bytesToStage!.Value - bytesStaged!.Value) : 0;

        if (remainingDownload > 0)
        {
            return (DownloadStatus.Downloading, DownloadStatus.Unknown, CalculateProgress(bytesToDownload, bytesDownloaded, bytesToStage, bytesStaged));
        }

        if (remainingStage > 0)
        {
            return (DownloadStatus.Idle, DownloadStatus.Installing, CalculateProgress(bytesToDownload, bytesDownloaded, bytesToStage, bytesStaged));
        }

        if (stateFlags.HasValue)
        {
            return SteamStateFlags.Interpret(stateFlags.Value);
        }

        return (DownloadStatus.Idle, DownloadStatus.Idle, 100.0);
    }

    private static double CalculateProgress(
        long? bytesToDownload,
        long? bytesDownloaded,
        long? bytesToStage,
        long? bytesStaged)
    {
        var total = (bytesToDownload ?? 0) + (bytesToStage ?? 0);
        var done = (bytesDownloaded ?? 0) + (bytesStaged ?? 0);

        if (total <= 0)
        {
            return 0;
        }

        return Math.Clamp((double)done / total * 100.0, 0, 100);
    }

    private static long? TryParseLongField(string trimmedLine)
    {
        var parts = trimmedLine.Split('"');
        if (parts.Length >= 4 && long.TryParse(parts[3], out var value))
        {
            return value;
        }
        return null;
    }

    private static string? ExtractAppIdFromFileName(string manifestFile)
    {
        var fileName = Path.GetFileNameWithoutExtension(manifestFile);
        if (fileName != null && fileName.StartsWith("appmanifest_", StringComparison.OrdinalIgnoreCase))
        {
            return fileName.Substring("appmanifest_".Length);
        }
        return null;
    }

    private void RefreshLibraryFolders()
    {
        var libraryFile = Path.Combine(_steamAppsPath, "libraryfolders.vdf");
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            _steamAppsPath
        };

        if (File.Exists(libraryFile))
        {
            try
            {
                var lines = File.ReadAllLines(libraryFile);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (!trimmed.Contains("\"path\"", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var parts = trimmed.Split('"');
                    if (parts.Length >= 4)
                    {
                        var path = parts[3].Replace("\\\\", "\\");
                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            var steamAppsPath = Path.Combine(path, "steamapps");
                            paths.Add(steamAppsPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error parsing Steam library folders: {ex.Message}");
            }
        }

        _steamAppsPaths.Clear();
        _downloadingPaths.Clear();
        foreach (var path in paths)
        {
            _steamAppsPaths.Add(path);
            _downloadingPaths.Add(Path.Combine(path, "downloading"));
        }
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
        @"for\s+(.+?)\s+-",
        RegexOptions.Compiled);

    private static readonly Regex AppIdUpdateChangedRegex = new(
        @"AppID\s+(?<id>\d+)\s+update changed\s*:\s*(?<state>.+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex AppIdUpdateStartedRegex = new(
        @"AppID\s+(?<id>\d+)\s+update started\s*:\s*download",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex AppIdStateChangedRegex = new(
        @"AppID\s+(?<id>\d+)\s+state changed\s*:\s*(?<state>.+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
