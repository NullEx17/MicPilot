using System.Windows;
using MicPilot.App.ViewModels;

namespace MicPilot.App.Views;

public partial class ProfilesWindow
{
    public ProfilesWindow(ProfilesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ProfilesViewModel viewModel)
        {
            viewModel.Save();
        }

        DialogResult = true;
        Close();
    }
}
