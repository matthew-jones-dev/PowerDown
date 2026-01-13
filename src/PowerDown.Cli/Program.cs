using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PowerDown.Abstractions;
using PowerDown.Core;
using PowerDown.Core.Services;
using PowerDown.Platform.Windows;
using PowerDown.Platform.Windows.Detectors;
using PowerDown.Platform.Windows.Services;

namespace PowerDown.Cli;

public class Program
{
    public static int Main(string[] args)
    {
        return MainAsync(args).GetAwaiter().GetResult();
    }

    public static async Task<int> MainAsync(string[] args)
    {
        var configuration = BuildConfiguration();
        var config = LoadConfiguration(configuration);
        var parseResult = ApplyCommandLineArguments(config, args);

        if (!parseResult.IsSuccess)
        {
            return 1;
        }

        if (config.Verbose)
        {
            Console.WriteLine("Configuration loaded:");
            Console.WriteLine($"  Verification Delay: {config.VerificationDelaySeconds}s");
            Console.WriteLine($"  Polling Interval: {config.PollingIntervalSeconds}s");
            Console.WriteLine($"  Required Checks: {config.RequiredNoActivityChecks}");
            Console.WriteLine($"  Monitor Steam: {config.MonitorSteam}");
            Console.WriteLine($"  Monitor Epic: {config.MonitorEpic}");
            Console.WriteLine($"  Dry Run: {config.DryRun}");
            Console.WriteLine($"  Verbose: {config.Verbose}");
            if (!string.IsNullOrEmpty(config.CustomSteamPath))
                Console.WriteLine($"  Custom Steam Path: {config.CustomSteamPath}");
            if (!string.IsNullOrEmpty(config.CustomEpicPath))
                Console.WriteLine($"  Custom Epic Path: {config.CustomEpicPath}");
            Console.WriteLine();
        }

        var services = new ServiceCollection();

        services.AddSingleton<ConsoleLogger>();
        services.AddSingleton(config);
        services.AddSingleton<WindowsPlatformDetector>();
        services.AddSingleton<ISteamPathDetector, SteamPathDetector>();
        services.AddSingleton<IEpicPathDetector, EpicPathDetector>();
        services.AddSingleton<WindowsShutdownService>();
        services.AddSingleton<IShutdownService>(sp => sp.GetRequiredService<WindowsShutdownService>());
        services.AddSingleton<IDetectorFactory, DetectorFactory>();
        services.AddTransient<DownloadOrchestrator>();

        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ConsoleLogger>();

        try
        {
            var detectors = serviceProvider.GetRequiredService<IDetectorFactory>().CreateDetectors(config, logger);
            var orchestrator = serviceProvider.GetRequiredService<DownloadOrchestrator>();
            await orchestrator.MonitorAndShutdownAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] {ex.Message}");
            if (config.Verbose)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return 1;
        }
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables(prefix: "POWERDOWN_");

        return builder.Build();
    }

    private static Configuration LoadConfiguration(IConfigurationRoot configuration)
    {
        var config = new Configuration();

        var section = configuration.GetSection("PowerDown");
        if (section.Exists())
        {
            section.Bind(config);
        }

        if (int.TryParse(Environment.GetEnvironmentVariable("POWERDOWN_VERIFICATIONDELAY"), out var delay))
            config.VerificationDelaySeconds = delay;
        if (int.TryParse(Environment.GetEnvironmentVariable("POWERDOWN_POLLINGINTERVAL"), out var interval))
            config.PollingIntervalSeconds = interval;
        if (int.TryParse(Environment.GetEnvironmentVariable("POWERDOWN_REQUIREDCHECKS"), out var checks))
            config.RequiredNoActivityChecks = checks;
        if (int.TryParse(Environment.GetEnvironmentVariable("POWERDOWN_SHUTDOWNDELAY"), out var shutdownDelay))
            config.ShutdownDelaySeconds = shutdownDelay;
        if (bool.TryParse(Environment.GetEnvironmentVariable("POWERDOWN_MONITORSTEAM"), out var steam))
            config.MonitorSteam = steam;
        if (bool.TryParse(Environment.GetEnvironmentVariable("POWERDOWN_MONITOREPIC"), out var epic))
            config.MonitorEpic = epic;
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("POWERDOWN_STEAMPATH")))
            config.CustomSteamPath = Environment.GetEnvironmentVariable("POWERDOWN_STEAMPATH");
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("POWERDOWN_EPICPATH")))
            config.CustomEpicPath = Environment.GetEnvironmentVariable("POWERDOWN_EPICPATH");
        if (bool.TryParse(Environment.GetEnvironmentVariable("POWERDOWN_DRYRUN"), out var dryrun))
            config.DryRun = dryrun;
        if (bool.TryParse(Environment.GetEnvironmentVariable("POWERDOWN_VERBOSE"), out var verbose))
            config.Verbose = verbose;

        return config;
    }

    private static ParseResult ApplyCommandLineArguments(Configuration config, string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg == "--help" || arg == "-h")
            {
                PrintHelp();
                return new ParseResult { ShouldExit = true };
            }
            else if (arg == "--delay" || arg == "-d")
            {
                if (i + 1 >= args.Length)
                {
                    var error = $"'--delay' requires a value";
                    PrintError(error);
                    return new ParseResult { IsSuccess = false, ErrorMessage = error };
                }
                var value = args[i + 1];
                if (!int.TryParse(value, out var delay))
                {
                    var error = $"'--delay' '{value}' is not a valid number";
                    PrintError(error);
                    return new ParseResult { IsSuccess = false, ErrorMessage = error };
                }
                if (delay <= 0)
                {
                    var error = $"'--delay' must be greater than 0, got {delay}";
                    PrintError(error);
                    return new ParseResult { IsSuccess = false, ErrorMessage = error };
                }
                config.VerificationDelaySeconds = delay;
                i++;
            }
            else if (arg == "--interval" || arg == "-i")
            {
                if (i + 1 >= args.Length)
                {
                    var error = $"'--interval' requires a value";
                    PrintError(error);
                    return new ParseResult { IsSuccess = false, ErrorMessage = error };
                }
                var value = args[i + 1];
                if (!int.TryParse(value, out var interval))
                {
                    var error = $"'--interval' '{value}' is not a valid number";
                    PrintError(error);
                    return new ParseResult { IsSuccess = false, ErrorMessage = error };
                }
                if (interval <= 0)
                {
                    var error = $"'--interval' must be greater than 0, got {interval}";
                    PrintError(error);
                    return new ParseResult { IsSuccess = false, ErrorMessage = error };
                }
                config.PollingIntervalSeconds = interval;
                i++;
            }
            else if (arg == "--checks" || arg == "-c")
            {
                if (i + 1 >= args.Length)
                {
                    var error = $"'--checks' requires a value";
                    PrintError(error);
                    return new ParseResult { IsSuccess = false, ErrorMessage = error };
                }
                var value = args[i + 1];
                if (!int.TryParse(value, out var checks))
                {
                    var error = $"'--checks' '{value}' is not a valid number";
                    PrintError(error);
                    return new ParseResult { IsSuccess = false, ErrorMessage = error };
                }
                if (checks <= 0)
                {
                    var error = $"'--checks' must be greater than 0, got {checks}";
                    PrintError(error);
                    return new ParseResult { IsSuccess = false, ErrorMessage = error };
                }
                config.RequiredNoActivityChecks = checks;
                i++;
            }
            else if (arg == "--shutdown-delay")
            {
                if (i + 1 >= args.Length)
                {
                    var error = $"'--shutdown-delay' requires a value";
                    PrintError(error);
                    return new ParseResult { IsSuccess = false, ErrorMessage = error };
                }
                var value = args[i + 1];
                if (!int.TryParse(value, out var shutdownDelay))
                {
                    var error = $"'--shutdown-delay' '{value}' is not a valid number";
                    PrintError(error);
                    return new ParseResult { IsSuccess = false, ErrorMessage = error };
                }
                if (shutdownDelay <= 0)
                {
                    var error = $"'--shutdown-delay' must be greater than 0, got {shutdownDelay}";
                    PrintError(error);
                    return new ParseResult { IsSuccess = false, ErrorMessage = error };
                }
                config.ShutdownDelaySeconds = shutdownDelay;
                i++;
            }
            else if (arg == "--steam-only" || arg == "-s")
            {
                config.MonitorSteam = true;
                config.MonitorEpic = false;
            }
            else if (arg == "--epic-only" || arg == "-e")
            {
                config.MonitorSteam = false;
                config.MonitorEpic = true;
            }
            else if (arg == "--steam-path")
            {
                if (i + 1 >= args.Length)
                {
                    var error = $"'--steam-path' requires a value";
                    PrintError(error);
                    return new ParseResult { IsSuccess = false, ErrorMessage = error };
                }
                config.CustomSteamPath = args[i + 1];
                i++;
            }
            else if (arg == "--epic-path")
            {
                if (i + 1 >= args.Length)
                {
                    var error = $"'--epic-path' requires a value";
                    PrintError(error);
                    return new ParseResult { IsSuccess = false, ErrorMessage = error };
                }
                config.CustomEpicPath = args[i + 1];
                i++;
            }
            else if (arg == "--dry-run" || arg == "-r")
            {
                config.DryRun = true;
            }
            else if (arg == "--verbose" || arg == "-v")
            {
                config.Verbose = true;
            }
            else if (arg == "--config")
            {
                if (i + 1 >= args.Length)
                {
                    var error = $"'--config' requires a value";
                    PrintError(error);
                    return new ParseResult { IsSuccess = false, ErrorMessage = error };
                }
                var configPath = args[i + 1];
                if (!File.Exists(configPath))
                {
                    var error = $"'--config' file not found: '{configPath}'";
                    PrintError(error);
                    return new ParseResult { IsSuccess = false, ErrorMessage = error };
                }
                try
                {
                    var customConfig = new ConfigurationBuilder()
                        .AddJsonFile(configPath)
                        .Build();
                    customConfig.GetSection("PowerDown").Bind(config);
                }
                catch (Exception ex)
                {
                    var error = $"'--config' failed to parse: {ex.Message}";
                    PrintError(error);
                    return new ParseResult { IsSuccess = false, ErrorMessage = error };
                }
                i++;
            }
            else if (arg.StartsWith("-"))
            {
                var error = $"unknown option '{arg}'";
                PrintError(error);
                return new ParseResult { IsSuccess = false, ErrorMessage = error };
            }
        }

        return new ParseResult { IsSuccess = true };
    }

    private static void PrintError(string message)
    {
        Console.Error.WriteLine($"PowerDown: error: {message}");
    }

    private static void PrintHelp()
    {
        Console.WriteLine("PowerDown - Auto-shutdown when game downloads complete");
        Console.WriteLine();
        Console.WriteLine("Usage: PowerDown [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --delay, -d <seconds>        Verification delay in seconds (must be > 0, default: 60)");
        Console.WriteLine("  --interval, -i <seconds>     Polling interval in seconds (must be > 0, default: 10)");
        Console.WriteLine("  --checks, -c <number>        Required consecutive idle checks (must be > 0, default: 3)");
        Console.WriteLine("  --shutdown-delay <seconds>   Delay before shutdown (must be > 0, default: 30)");
        Console.WriteLine("  --steam-only, -s             Monitor Steam only (overrides Epic detection)");
        Console.WriteLine("  --epic-only, -e              Monitor Epic only (overrides Steam detection)");
        Console.WriteLine("  --steam-path <path>          Custom Steam install directory");
        Console.WriteLine("  --epic-path <path>           Custom Epic Games install directory");
        Console.WriteLine("  --dry-run, -r                Test mode without actual shutdown");
        Console.WriteLine("  --verbose, -v                Enable verbose logging");
        Console.WriteLine("  --config <path>              Path to JSON config file");
        Console.WriteLine("  --help, -h                   Show this help message");
        Console.WriteLine();
        Console.WriteLine("Configuration sources (in order of precedence):");
        Console.WriteLine("  1. Command-line arguments (highest priority)");
        Console.WriteLine("  2. Environment variables (POWERDOWN_*)");
        Console.WriteLine("  3. appsettings.json");
        Console.WriteLine("  4. Defaults");
        Console.WriteLine();
        Console.WriteLine("Environment variables:");
        Console.WriteLine("  POWERDOWN_VERIFICATIONDELAY  Verification delay in seconds (must be > 0)");
        Console.WriteLine("  POWERDOWN_POLLINGINTERVAL    Polling interval in seconds (must be > 0)");
        Console.WriteLine("  POWERDOWN_REQUIREDCHECKS     Required consecutive idle checks (must be > 0)");
        Console.WriteLine("  POWERDOWN_SHUTDOWNDELAY      Delay before shutdown in seconds (must be > 0)");
        Console.WriteLine("  POWERDOWN_MONITORSTEAM       Monitor Steam (true/false)");
        Console.WriteLine("  POWERDOWN_MONITOREPIC        Monitor Epic (true/false)");
        Console.WriteLine("  POWERDOWN_STEAMPATH          Custom Steam install directory");
        Console.WriteLine("  POWERDOWN_EPICPATH           Custom Epic Games install directory");
        Console.WriteLine("  POWERDOWN_DRYRUN             Test mode without actual shutdown");
        Console.WriteLine("  POWERDOWN_VERBOSE            Enable verbose logging");
        Console.WriteLine();
        Console.WriteLine("By default, PowerDown attempts to detect both Steam and Epic Games.");
        Console.WriteLine("Use --steam-only or --epic-only to monitor a specific launcher.");
        Console.WriteLine("If automatic detection fails, provide paths with --steam-path and --epic-path.");
        Console.WriteLine();
        Console.WriteLine("Error examples:");
        Console.WriteLine("  PowerDown: error: '--delay' 'abc' is not a valid number");
        Console.WriteLine("  PowerDown: error: '--delay' must be greater than 0, got -5");
        Console.WriteLine("  PowerDown: error: '--delay' requires a value");
        Console.WriteLine("  PowerDown: error: unknown option '--invalid'");
        Console.WriteLine("  PowerDown: error: '--config' file not found: '/path/config.json'");
    }
}

public class ParseResult
{
    public bool IsSuccess { get; set; } = true;
    public bool ShouldExit { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IDetectorFactory
{
    List<IDownloadDetector> CreateDetectors(Configuration config, ConsoleLogger logger);
}

public class DetectorFactory : IDetectorFactory
{
    private readonly ISteamPathDetector _steamPathDetector;
    private readonly IEpicPathDetector _epicPathDetector;

    public DetectorFactory(ISteamPathDetector steamPathDetector, IEpicPathDetector epicPathDetector)
    {
        _steamPathDetector = steamPathDetector;
        _epicPathDetector = epicPathDetector;
    }

    public List<IDownloadDetector> CreateDetectors(Configuration config, ConsoleLogger logger)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        var detectors = new List<IDownloadDetector>();

        if (config.MonitorSteam)
        {
            var steamPath = _steamPathDetector.DetectSteamPath(config.CustomSteamPath);
            if (steamPath != null)
            {
                var detector = new SteamDownloadDetector(steamPath, logger);
                detectors.Add(detector);
                logger.LogInfo($"Steam detected at: {steamPath}");
            }
            else
            {
                logger.LogWarning("Steam path not found - Steam monitoring disabled");
            }
        }

        if (config.MonitorEpic)
        {
            var epicPath = _epicPathDetector.DetectEpicPath(config.CustomEpicPath);
            if (epicPath != null)
            {
                var detector = new EpicDownloadDetector(epicPath, logger);
                detectors.Add(detector);
                logger.LogInfo($"Epic Games detected at: {epicPath}");
            }
            else
            {
                logger.LogWarning("Epic Games path not found - Epic monitoring disabled");
            }
        }

        if (!detectors.Any())
        {
            logger.LogWarning("No launchers found. System will exit after checking for downloads.");
        }

        return detectors;
    }
}
