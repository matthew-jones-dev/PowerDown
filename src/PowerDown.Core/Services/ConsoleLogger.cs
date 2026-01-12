using System;

namespace PowerDown.Core;

public class ConsoleLogger
{
    private readonly bool _verbose;

    public ConsoleLogger(bool verbose = false)
    {
        _verbose = verbose;
    }

    public void LogInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"[INFO] {message}");
        Console.ResetColor();
    }

    public void LogWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[WARN] {message}");
        Console.ResetColor();
    }

    public void LogError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR] {message}");
        Console.ResetColor();
    }

    public void LogVerbose(string message)
    {
        if (_verbose)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"[VERBOSE] {message}");
            Console.ResetColor();
        }
    }

    public void LogSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[SUCCESS] {message}");
        Console.ResetColor();
    }
}
