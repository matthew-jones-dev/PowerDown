using System;
using System.IO;
using PowerDown.Core.Services;

namespace PowerDown.Core.Helpers;

/// <summary>
/// Validates custom paths for Steam and Epic launchers.
/// Provides consistent path validation across all platform detectors.
/// </summary>
public static class PathValidator
{
    public const string SteamLauncher = "Steam";
    public const string EpicLauncher = "Epic Games";

    /// <summary>
    /// Validates and resolves a custom launcher path.
    /// </summary>
    /// <param name="customPath">The custom path provided by the user (may be null or empty).</param>
    /// <param name="launcherName">The name of the launcher for error messages.</param>
    /// <param name="logger">The logger for warning messages.</param>
    /// <param name="getDefaultPath">Function to get the default path.</param>
    /// <returns>The validated path, or null if validation fails.</returns>
    public static string? ValidateAndResolve(
        string? customPath,
        string launcherName,
        ConsoleLogger logger,
        Func<string?> getDefaultPath)
    {
        if (string.IsNullOrWhiteSpace(customPath))
        {
            return getDefaultPath();
        }

        if (Directory.Exists(customPath))
        {
            return Path.GetFullPath(customPath);
        }

        logger.LogWarning($"Custom {launcherName} path does not exist: {customPath}");
        return null;
    }

    /// <summary>
    /// Validates Steam path with Steam-specific error messages.
    /// </summary>
    public static string? ValidateSteamPath(
        string? customPath,
        ConsoleLogger logger,
        Func<string?> getDefaultPath)
    {
        return ValidateAndResolve(customPath, SteamLauncher, logger, getDefaultPath);
    }

    /// <summary>
    /// Validates Epic Games path with Epic-specific error messages.
    /// </summary>
    public static string? ValidateEpicPath(
        string? customPath,
        ConsoleLogger logger,
        Func<string?> getDefaultPath)
    {
        return ValidateAndResolve(customPath, EpicLauncher, logger, getDefaultPath);
    }

    /// <summary>
    /// Checks if a path exists and logs appropriate warnings.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <param name="pathDescription">Description for error messages.</param>
    /// <param name="logger">The logger for warnings.</param>
    /// <returns>True if the path exists, false otherwise.</returns>
    public static bool PathExists(
        string? path,
        string pathDescription,
        ConsoleLogger logger)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            logger.LogWarning($"{pathDescription} path not found");
            return false;
        }

        if (!Directory.Exists(path))
        {
            logger.LogWarning($"{pathDescription} directory does not exist: {path}");
            return false;
        }

        return true;
    }
}
