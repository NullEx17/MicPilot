using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MicPilot.App.Services;
using MicPilot.Core.Models;
using MicPilot.Core.Settings;
using MicPilot.Hotkeys;
using MicPilot.Profiles;

namespace MicPilot.App.ViewModels;

public sealed class ProfilesViewModel : ViewModelBase
{
    private readonly AppSettings _settings;
    private readonly ProcessWatcher _processWatcher = new();
    private readonly DispatcherTimer _runningTimer;
    private ProfileItemViewModel? _selectedProfile;

    public ProfilesViewModel(AppSettings settings)
    {
        _settings = settings;
        Profiles = new ObservableCollection<ProfileItemViewModel>(
            settings.Profiles.Select(CreateItem));

        SelectedProfile = Profiles.FirstOrDefault(profile => profile.Id == settings.ActiveProfileId)
                         ?? Profiles.FirstOrDefault();

        AddCommand = new RelayCommand(AddBlankProfile);
        DeleteCommand = new RelayCommand(DeleteSelected, () => SelectedProfile is not null);
        SetActiveCommand = new RelayCommand(SetActive, () => SelectedProfile is not null);
        BrowseProcessCommand = new RelayCommand(BrowseProcess, () => SelectedProfile is not null);

        _runningTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _runningTimer.Tick += (_, _) =>
        {
            foreach (var profile in Profiles)
            {
                profile.RefreshRunning();
            }
        };
        _runningTimer.Start();
    }

    public ObservableCollection<ProfileItemViewModel> Profiles { get; }

    public ProfileItemViewModel? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (SetProperty(ref _selectedProfile, value))
            {
                OnPropertyChanged(nameof(HasSelection));
            }
        }
    }

    public bool HasSelection => SelectedProfile is not null;

    public IReadOnlyList<string> HotkeyOptions => HotkeyParser.SuggestedHotkeys;

    public IReadOnlyList<HotkeyMode> ModeOptions { get; } =
        [HotkeyMode.Toggle, HotkeyMode.WalkieTalkie];

    public ICommand AddCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SetActiveCommand { get; }
    public ICommand BrowseProcessCommand { get; }

    public void Save()
    {
        _settings.Profiles = Profiles.Select(item => item.ToProfile()).ToList();

        if (_settings.ActiveProfileId is Guid activeId &&
            _settings.Profiles.All(profile => profile.Id != activeId))
        {
            _settings.ActiveProfileId = _settings.Profiles.FirstOrDefault()?.Id;
        }

        SettingsStore.Save(_settings);
    }

    public void AddProfile(Profile profile)
    {
        var item = CreateItem(profile);
        Profiles.Add(item);
        SelectedProfile = item;
    }

    private void AddBlankProfile()
    {
        AddProfile(new Profile
        {
            Name = "New Profile",
            ProcessName = "",
            Hotkey = "PgDn",
            Mode = HotkeyMode.Toggle,
            Enabled = true
        });
    }

    public void DeleteSelected()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        var index = Profiles.IndexOf(SelectedProfile);
        Profiles.Remove(SelectedProfile);
        SelectedProfile = Profiles.ElementAtOrDefault(Math.Max(0, index - 1))
                          ?? Profiles.FirstOrDefault();
    }

    private void SetActive()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        _settings.ActiveProfileId = SelectedProfile.Id;
        foreach (var profile in Profiles)
        {
            profile.IsActive = profile.Id == SelectedProfile.Id;
        }
    }

    private void BrowseProcess()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*",
            Title = "Select application executable"
        };

        if (dialog.ShowDialog() == true)
        {
            SelectedProfile.ProcessName = Path.GetFileName(dialog.FileName);
            GameIconResolver.Remember(SelectedProfile.ProcessName, dialog.FileName);
            if (string.IsNullOrWhiteSpace(SelectedProfile.Name) || SelectedProfile.Name == "New Profile")
            {
                SelectedProfile.Name = Path.GetFileNameWithoutExtension(dialog.FileName);
            }
        }
    }

    private ProfileItemViewModel CreateItem(Profile profile) =>
        new(profile, _settings.ActiveProfileId == profile.Id, _processWatcher);
}

public sealed class ProfileItemViewModel : ViewModelBase
{
    private readonly ProcessWatcher _processWatcher;
    private string _name;
    private string _processName;
    private string _hotkey;
    private HotkeyMode _mode;
    private bool _autoActivate;
    private bool _enabled;
    private bool _isActive;

    public ProfileItemViewModel(Profile profile, bool isActive, ProcessWatcher processWatcher)
    {
        Id = profile.Id;
        _name = profile.Name;
        _processName = profile.ProcessName;
        _hotkey = profile.Hotkey;
        _mode = profile.Mode;
        _autoActivate = profile.AutoActivate;
        _enabled = profile.Enabled;
        _isActive = isActive;
        _processWatcher = processWatcher;
    }

    public Guid Id { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                OnPropertyChanged(nameof(Icon));
                OnPropertyChanged(nameof(HasIcon));
            }
        }
    }

    public ImageSource? Icon => GameIconResolver.Resolve(ProcessName, Name);

    public bool HasIcon => Icon is not null;

    public string ProcessName
    {
        get => _processName;
        set
        {
            if (SetProperty(ref _processName, value))
            {
                GameIconResolver.Invalidate(value);
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(Icon));
                OnPropertyChanged(nameof(HasIcon));
            }
        }
    }

    public string Hotkey
    {
        get => _hotkey;
        set => SetProperty(ref _hotkey, value);
    }

    public HotkeyMode Mode
    {
        get => _mode;
        set => SetProperty(ref _mode, value);
    }

    public bool AutoActivate
    {
        get => _autoActivate;
        set => SetProperty(ref _autoActivate, value);
    }

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (SetProperty(ref _isActive, value))
            {
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    public bool IsRunning => _processWatcher.IsProcessRunning(ProcessName);

    public string RunningText => IsRunning ? "Running" : "Not running";

    public string StatusText
    {
        get
        {
            var parts = new List<string>();
            if (IsActive)
            {
                parts.Add("Active");
            }

            parts.Add(RunningText);
            return string.Join(" · ", parts);
        }
    }

    public void RefreshRunning()
    {
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(RunningText));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(Icon));
        OnPropertyChanged(nameof(HasIcon));
    }

    public Profile ToProfile() => new()
    {
        Id = Id,
        Name = Name.Trim(),
        ProcessName = ProcessName.Trim(),
        Hotkey = Hotkey.Trim(),
        Mode = Mode,
        AutoActivate = AutoActivate,
        Enabled = Enabled
    };
}
