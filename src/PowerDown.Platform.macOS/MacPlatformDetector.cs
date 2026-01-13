using System;
using PowerDown.Abstractions;

namespace PowerDown.Platform.macOS;

public class MacPlatformDetector : IPlatformDetector
{
    public bool IsSupported() => OperatingSystem.IsMacOS();
    
    public string GetPlatformName() => "macOS";
}
