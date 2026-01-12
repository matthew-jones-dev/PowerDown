using System;
using PowerDown.Abstractions;

namespace PowerDown.Platform.Windows;

public class WindowsPlatformDetector : IPlatformDetector
{
    public bool IsSupported() => OperatingSystem.IsWindows();
    public string GetPlatformName() => "Windows";
}
