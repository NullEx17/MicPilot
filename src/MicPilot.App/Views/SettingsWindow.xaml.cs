using System.Diagnostics;
using System.IO;
using System.Windows;
using MicPilot.App.ViewModels;
using MicPilot.Diagnostics;
using MessageBox = System.Windows.MessageBox;
using Clipboard = System.Windows.Clipboard;

namespace MicPilot.App.Views;

public partial class SettingsWindow
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            if (!viewModel.Save())
            {
                MessageBox.Show(
                    viewModel.SaveError ?? "Could not save settings.",
                    "MicPilot",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
        }

        DialogResult = true;
        Close();
    }

    private void OpenLogs_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(Log.LogDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = Log.LogDirectory,
            UseShellExecute = true
        });
    }

    private void CopyDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(Log.GetDiagnosticsText());
        MessageBox.Show(
            "Diagnostics copied to clipboard.",
            "MicPilot",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
