using System.ComponentModel;
using System.Runtime.CompilerServices;
using PowerDown.Abstractions;
using PowerDown.Abstractions.Interfaces;

namespace PowerDown.UI.ViewModels;

public class DownloadItemViewModel : INotifyPropertyChanged
{
    private string _gameName = string.Empty;
    private string _launcherName = string.Empty;
    private double _progress;
    private string _statusText = "Unknown";
    private bool _isActive;
    private string _stateGlyph = "●";
    private string _stateColor = "#94A3B8";
    
    public string GameName
    {
        get => _gameName;
        set { _gameName = value; OnPropertyChanged(); }
    }
    
    public string LauncherName
    {
        get => _launcherName;
        set { _launcherName = value; OnPropertyChanged(); }
    }
    
    public double Progress
    {
        get => _progress;
        set { _progress = value; OnPropertyChanged(); }
    }

    public bool IsActive
    {
        get => _isActive;
        private set { _isActive = value; OnPropertyChanged(); }
    }

    public string StateGlyph
    {
        get => _stateGlyph;
        private set { _stateGlyph = value; OnPropertyChanged(); }
    }

    public string StateColor
    {
        get => _stateColor;
        private set { _stateColor = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }
    
    public DownloadItemViewModel() { }
    
    public DownloadItemViewModel(DownloadUpdate update)
    {
        UpdateFrom(update);
    }
    
    public void UpdateFrom(DownloadUpdate update)
    {
        GameName = update.GameName;
        LauncherName = update.LauncherName;
        Progress = update.Progress;
        IsActive = update.Status is DownloadStatus.Downloading or DownloadStatus.Installing;
        StatusText = update.Status switch
        {
            DownloadStatus.Downloading => "Downloading",
            DownloadStatus.Installing => "Installing",
            DownloadStatus.Idle => "Completed",
            DownloadStatus.Error => "Error",
            _ => "Unknown"
        };

        StateColor = update.Status switch
        {
            DownloadStatus.Downloading => "#37D4A2",
            DownloadStatus.Installing => "#FFB347",
            DownloadStatus.Idle => "#46B1F2",
            DownloadStatus.Error => "#F26C6C",
            _ => "#94A3B8"
        };
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
