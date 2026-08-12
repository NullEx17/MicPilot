using System.Windows;
using MicPilot.App.Services;
using MicPilot.App.ViewModels;
using MessageBox = System.Windows.MessageBox;

namespace MicPilot.App.Views;

public partial class FirstRunWindow
{
    public FirstRunWindow(FirstRunViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is FirstRunViewModel vm && vm.Save())
        {
            DialogResult = true;
            Close();
            return;
        }

        MessageBox.Show(
            "Choose a microphone and a valid hotkey to continue.",
            "MicPilot",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OpenSound_Click(object sender, RoutedEventArgs e) => WindowsAudioSettings.Open();
}
