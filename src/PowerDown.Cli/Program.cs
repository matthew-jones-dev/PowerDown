using System;
using System.CommandLine;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        var result = Task.Run(async () => await MainAsync(args)).GetAwaiter().GetResult();
        return result;
    }

    public static async Task<int> MainAsync(string[] args)
    {
        var services = new ServiceCollection();

        services.AddSingleton<ConsoleLogger>();
        services.AddSingleton<Configuration>();
        services.AddSingleton<WindowsPlatformDetector>();
        services.AddSingleton<WindowsShutdownService>();
        services.AddSingleton<IDetectorFactory, DetectorFactory>();
        services.AddTransient<DownloadOrchestrator>();

        var serviceProvider = services.BuildServiceProvider();

        var config = ParseArguments(args);
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

    private static Configuration ParseArguments(string[] args)
    {
        var config = new Configuration();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i].ToLower();

            if (arg == "--delay" || arg == "-d")
            {
                if (i + 1 < args.Length && int.TryParse(args[i + 1], out var delay))
                {
                    config.VerificationDelaySeconds = delay;
                }
            }
            else if (arg == "--interval" || arg == "-i")
            {
                if (i + 1 < args.Length && int.TryParse(args[i + 1], out var interval))
                {
                    config.PollingIntervalSeconds = interval;
                }
            }
            else if (arg == "--checks" || arg == "-c")
            {
                if (i + 1 < args.Length && int.TryParse(args[i + 1], out var checks))
                {
                    config.RequiredNoActivityChecks = checks;
                }
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
                if (i + 1 < args.Length)
                {
                    config.CustomSteamPath = args[i + 1];
                }
            }
            else if (arg == "--epic-path")
            {
                if (i + 1 < args.Length)
                {
                    config.CustomEpicPath = args[i + 1];
                }
            }
            else if (arg == "--dry-run" || arg == "-r")
            {
                config.DryRun = true;
            }
            else if (arg == "--verbose" || arg == "-v")
            {
                config.Verbose = true;
            }
            else if (arg == "--help" || arg == "-h")
            {
                PrintHelp();
            }
        }

        return config;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("PowerDown - Auto-shutdown when game downloads complete");
        Console.WriteLine();
        Console.WriteLine("Usage: PowerDown [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --delay, -d <seconds>     Verification delay in seconds (default: 60)");
        Console.WriteLine("  --interval, -i <seconds>  Polling interval in seconds (default: 10)");
        Console.WriteLine("  --checks, -c <number>        Required consecutive idle checks (default: 3)");
        Console.WriteLine("  --steam-only, -s              Monitor Steam only (overrides Epic detection)");
        Console.WriteLine("  --epic-only, -e               Monitor Epic only (overrides Steam detection)");
        Console.WriteLine("  --steam-path <path>           Custom Steam install directory");
        Console.WriteLine("  --epic-path <path>            Custom Epic Games install directory");
        Console.WriteLine("  --dry-run, -r                     Test mode without actual shutdown");
        Console.WriteLine("  --verbose, -v                   Enable verbose logging");
        Console.WriteLine("  --help, -h                          Show this help message");
        Console.WriteLine();
        Console.WriteLine("By default, PowerDown attempts to detect both Steam and Epic Games.");
        Console.WriteLine("Use --steam-only or --epic-only to monitor a specific launcher.");
        Console.WriteLine("If automatic detection fails, provide paths with --steam-path and --epic-path.");
    }
}

public interface IDetectorFactory
{
    List<IDownloadDetector> CreateDetectors(Configuration config, ConsoleLogger logger);
}

public class DetectorFactory : IDetectorFactory
{
    private readonly ConsoleLogger _logger;

    public DetectorFactory(ConsoleLogger logger)
    {
        _logger = logger;
    }

    public List<IDownloadDetector> CreateDetectors(Configuration config, ConsoleLogger logger)
    {
        var detectors = new List<IDownloadDetector>();

        if (config.MonitorSteam)
        {
            var steamPath = SteamPathDetector.DetectSteamPath(config.CustomSteamPath);
            if (steamPath != null)
            {
                var detector = new SteamDownloadDetector(steamPath);
                detectors.Add(detector);
                _logger.LogInfo($"Steam detected at: {steamPath}");
            }
            else
            {
                _logger.LogWarning("Steam path not found - Steam monitoring disabled");
            }
        }

        if (config.MonitorEpic)
        {
            var epicPath = EpicPathDetector.DetectEpicPath(config.CustomEpicPath);
            if (epicPath != null)
            {
                var detector = new EpicDownloadDetector(epicPath);
                detectors.Add(detector);
                _logger.LogInfo($"Epic Games detected at: {epicPath}");
            }
            else
            {
                _logger.LogWarning("Epic Games path not found - Epic monitoring disabled");
            }
        }

        if (!detectors.Any())
        {
            _logger.LogWarning("No launchers found. System will exit after checking for downloads.");
        }

        return detectors;
    }
}
