using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using PowerDown.UI.ViewModels;

namespace PowerDown.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private SettingsWindow? _settingsWindow;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        
        Closing += OnWindowClosing;
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_viewModel.IsMonitoring)
        {
            e.Cancel = true;
            _viewModel.CancelCommand.Execute(null);
        }
    }

    private void OnOpenSettings(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_settingsWindow == null)
        {
            _settingsWindow = new SettingsWindow
            {
                DataContext = DataContext
            };
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.Show(this);
            return;
        }

        _settingsWindow.Activate();
    }
}
