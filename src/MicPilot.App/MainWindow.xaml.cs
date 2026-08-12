using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using MicPilot.App.Overlay;
using MicPilot.App.Tray;
using MicPilot.App.ViewModels;
using MicPilot.App.Views;
using MicPilot.App.Views.Pages;

namespace MicPilot.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private StatusOverlayWindow? _overlay;
    private TrayIconService? _tray;
    private bool _reallyClosing;
    private ProfilesViewModel? _profilesViewModel;
    private SettingsViewModel? _settingsViewModel;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        HomePage.DataContext = _viewModel;

        SourceInitialized += (_, _) =>
        {
            var handle = new WindowInteropHelper(this).Handle;
            _viewModel.AttachHotkeys(handle);
            NativeMethods.EnableRoundedCorners(handle);
        };

        Loaded += (_, _) =>
        {
            try
            {
                _overlay ??= new StatusOverlayWindow();
                InitializeTray();
                MaybeShowFirstRun();
                _overlay.RefreshAlwaysVisible(_viewModel.GameMicState, _viewModel.Settings);
                UpdateTrayState();

                if (_viewModel.Settings.StartMinimized && _viewModel.Settings.MinimizeToTray)
                {
                    Hide();
                }
            }
            catch (Exception ex)
            {
                MicPilot.Diagnostics.Log.Error($"MainWindow Loaded failed: {ex}");
                System.Windows.MessageBox.Show(
                    $"MicPilot started, but part of the UI failed to initialize:\n\n{ex.Message}",
                    "MicPilot",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        };

        StateChanged += (_, _) => UpdateMaximizeGlyph();
        Closing += OnClosing;
        Closed += (_, _) =>
        {
            _overlay?.Close();
            _tray?.Dispose();
            _viewModel.Dispose();
        };

        _viewModel.OpenProfilesRequested += () => NavigateToGames();
        _viewModel.OpenSettingsRequested += () => NavigateToSettings();
        _viewModel.GameMicStateChanged += OnGameMicStateChanged;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.NotificationRequested += (title, message) =>
            _tray?.ShowNotification(title, message, _viewModel.Settings.NotificationsEnabled);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.AudioEngineRunning)
            or nameof(MainViewModel.ActiveProfileName)
            or nameof(MainViewModel.ActiveProfileHotkey))
        {
            UpdateTrayState();
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void More_Click(object sender, RoutedEventArgs e) => MorePopup.IsOpen = true;

    private void MoreSettings_Click(object sender, RoutedEventArgs e)
    {
        MorePopup.IsOpen = false;
        NavigateToSettings();
    }

    private void MoreAbout_Click(object sender, RoutedEventArgs e)
    {
        MorePopup.IsOpen = false;
        ShowFromTray();
        NavAbout.IsChecked = true;
    }

    private void MoreExit_Click(object sender, RoutedEventArgs e)
    {
        MorePopup.IsOpen = false;
        _reallyClosing = true;
        Close();
    }

    private void UpdateMaximizeGlyph()
    {
        if (MaximizeButton is not null)
        {
            MaximizeButton.ToolTip = WindowState == WindowState.Maximized ? "Restore" : "Maximize";
        }

        if (RootChrome is not null)
        {
            RootChrome.Margin = WindowState == WindowState.Maximized ? new Thickness(8) : new Thickness(0);
            RootChrome.BorderThickness = WindowState == WindowState.Maximized ? new Thickness(0) : new Thickness(1);
            RootChrome.CornerRadius = WindowState == WindowState.Maximized ? new CornerRadius(0) : new CornerRadius(10);
        }
    }

    private static class NativeMethods
    {
        private const int DwmwaWindowCornerPreference = 33;
        private const int DwmwcpRound = 2;

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public static void EnableRoundedCorners(IntPtr hwnd)
        {
            var preference = DwmwcpRound;
            _ = DwmSetWindowAttribute(hwnd, DwmwaWindowCornerPreference, ref preference, sizeof(int));
        }
    }

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        if (NavHome.IsChecked == true)
        {
            ShowPage(home: true);
        }
        else if (NavGames.IsChecked == true)
        {
            EnsureGamesPage();
            ShowPage(games: true);
        }
        else if (NavSettings.IsChecked == true)
        {
            EnsureSettingsPage();
            ShowPage(settings: true);
        }
        else if (NavAbout.IsChecked == true)
        {
            ShowPage(about: true);
        }
    }

    private void ShowPage(bool home = false, bool games = false, bool settings = false, bool about = false)
    {
        HomePage.Visibility = home ? Visibility.Visible : Visibility.Collapsed;
        GamesHost.Visibility = games ? Visibility.Visible : Visibility.Collapsed;
        SettingsHost.Visibility = settings ? Visibility.Visible : Visibility.Collapsed;
        AboutPage.Visibility = about ? Visibility.Visible : Visibility.Collapsed;
    }

    private void EnsureGamesPage()
    {
        if (GamesHost.Content is not null)
        {
            return;
        }

        _profilesViewModel = new ProfilesViewModel(_viewModel.Settings);
        var page = new GamesAppsView(_profilesViewModel);
        page.Saved += () =>
        {
            _viewModel.ApplyProfiles();
            UpdateTrayState();
        };
        GamesHost.Content = page;
    }

    private void EnsureSettingsPage()
    {
        _settingsViewModel = new SettingsViewModel(
            _viewModel.Settings,
            _viewModel.GetPhysicalCaptureDevices(),
            _viewModel.GetVbCable());

        var page = new SettingsPageView(_settingsViewModel);
        page.Saved += () =>
        {
            _viewModel.ApplySettingsAndRestart();
            UpdateTrayState();
            EnsureOverlay()?.RefreshAlwaysVisible(_viewModel.GameMicState, _viewModel.Settings);
            if (!_viewModel.Settings.OverlayEnabled)
            {
                _overlay?.HideImmediate();
            }
        };
        SettingsHost.Content = page;
    }

    private void NavigateToGames()
    {
        ShowFromTray();
        NavGames.IsChecked = true;
    }

    private void NavigateToSettings()
    {
        ShowFromTray();
        NavSettings.IsChecked = true;
    }

    private void OnGameMicStateChanged()
    {
        UpdateTrayState();
        EnsureOverlay()?.ShowState(_viewModel.GameMicState, _viewModel.Settings);
    }

    private StatusOverlayWindow? EnsureOverlay()
    {
        try
        {
            return _overlay ??= new StatusOverlayWindow();
        }
        catch (Exception ex)
        {
            MicPilot.Diagnostics.Log.Error($"Overlay init failed: {ex}");
            return null;
        }
    }

    private void InitializeTray()
    {
        _tray?.Dispose();
        _tray = new TrayIconService();
        _tray.ToggleRequested += () => Dispatcher.Invoke(() => _viewModel.ToggleGameMicCommand.Execute(null));
        _tray.OpenRequested += () => Dispatcher.Invoke(() =>
        {
            ShowFromTray();
            NavHome.IsChecked = true;
        });
        _tray.ProfilesRequested += () => Dispatcher.Invoke(NavigateToGames);
        _tray.SettingsRequested += () => Dispatcher.Invoke(NavigateToSettings);
        _tray.ExitRequested += () => Dispatcher.Invoke(() =>
        {
            _reallyClosing = true;
            Close();
        });

        UpdateTrayState();
    }

    private void UpdateTrayState() =>
        _tray?.UpdateState(
            _viewModel.GameMicState,
            _viewModel.ActiveProfileName,
            _viewModel.AudioEngineRunning,
            _viewModel.ActiveProfileHotkey);

    private void MaybeShowFirstRun()
    {
        if (_viewModel.Settings.HasCompletedFirstRun)
        {
            return;
        }

        var wizard = new FirstRunWindow(
            new FirstRunViewModel(
                _viewModel.Settings,
                _viewModel.GetPhysicalCaptureDevices(),
                _viewModel.GetVbCable()))
        {
            Owner = this
        };

        if (wizard.ShowDialog() == true)
        {
            _viewModel.CompleteFirstRun();
            _viewModel.ApplySettingsAndRestart();
            UpdateTrayState();
        }
        else
        {
            _viewModel.CompleteFirstRun();
        }
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_reallyClosing || !_viewModel.Settings.MinimizeToTray)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }
}
