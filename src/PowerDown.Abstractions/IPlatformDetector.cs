namespace PowerDown.Abstractions;

public interface IPlatformDetector
{
    bool IsSupported();
    string GetPlatformName();
}
