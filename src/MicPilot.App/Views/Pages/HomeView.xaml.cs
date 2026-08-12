using System.Windows.Input;
using MicPilot.App.ViewModels;

namespace MicPilot.App.Views.Pages;

public partial class HomeView
{
    public HomeView()
    {
        InitializeComponent();
    }

    private void GameMicCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.ToggleGameMicCommand.CanExecute(null))
        {
            viewModel.ToggleGameMicCommand.Execute(null);
        }
    }
}
