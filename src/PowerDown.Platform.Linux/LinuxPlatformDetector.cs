using System;
using PowerDown.Abstractions;

namespace PowerDown.Platform.Linux;

public class LinuxPlatformDetector : IPlatformDetector
{
    public bool IsSupported() => OperatingSystem.IsLinux();
    
    public string GetPlatformName() => "Linux";
}
