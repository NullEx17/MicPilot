using System.IO;
using System.Windows;
using MicPilot.App.Services;
using MicPilot.App.ViewModels;
using MicPilot.Core.Models;
using MessageBox = System.Windows.MessageBox;

namespace MicPilot.App.Views.Dialogs;

public partial class ProfileEditorDialog
{
    public ProfileEditorDialog(ProfileItemViewModel? existing = null)
    {
        InitializeComponent();
        Existing = existing;

        HotkeyBox.Hotkey = existing?.Hotkey ?? "PgDn";

        if (existing is not null)
        {
            TitleText.Text = "Edit Game or App";
            ConfirmButton.Content = "Save";
            NameBox.Text = existing.Name;
            ProcessBox.Text = existing.ProcessName;
            AutoActivateBox.IsChecked = existing.AutoActivate;
            EnabledBox.IsChecked = existing.Enabled;
            WalkieModeBox.IsChecked = existing.Mode == HotkeyMode.WalkieTalkie;
            ToggleModeBox.IsChecked = existing.Mode != HotkeyMode.WalkieTalkie;
        }
    }

    public ProfileItemViewModel? Existing { get; }

    public string ProfileName => NameBox.Text.Trim();
    public string ProcessName => ProcessBox.Text.Trim();
    public string Hotkey => HotkeyBox.Hotkey;
    public HotkeyMode Mode => WalkieModeBox.IsChecked == true ? HotkeyMode.WalkieTalkie : HotkeyMode.Toggle;
    public bool AutoActivate => AutoActivateBox.IsChecked == true;
    public bool IsEnabledProfile => EnabledBox.IsChecked == true;

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*",
            Title = "Select application executable"
        };

        if (dialog.ShowDialog(this) == true)
        {
            ProcessBox.Text = Path.GetFileName(dialog.FileName);
            GameIconResolver.Remember(ProcessBox.Text, dialog.FileName);
            if (string.IsNullOrWhiteSpace(NameBox.Text) || NameBox.Text == "New Profile")
            {
                NameBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
            }
        }
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ProfileName))
        {
            MessageBox.Show(this, "Give this profile a name.", "MicPilot", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    public void ApplyTo(ProfileItemViewModel item)
    {
        item.Name = ProfileName;
        item.ProcessName = ProcessName;
        item.Hotkey = Hotkey;
        item.Mode = Mode;
        item.AutoActivate = AutoActivate;
        item.Enabled = IsEnabledProfile;
    }

    public Profile ToProfile() => new()
    {
        Name = string.IsNullOrWhiteSpace(ProfileName) ? "New Profile" : ProfileName,
        ProcessName = ProcessName,
        Hotkey = string.IsNullOrWhiteSpace(Hotkey) ? "PgDn" : Hotkey,
        Mode = Mode,
        AutoActivate = AutoActivate,
        Enabled = IsEnabledProfile
    };
}
