namespace PowerDown.Abstractions.Interfaces;

public interface ISteamPathDetector
{
    string? DetectSteamPath(string? customPath);
}
