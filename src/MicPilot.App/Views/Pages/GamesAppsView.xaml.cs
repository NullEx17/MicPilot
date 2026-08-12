using System.Windows;
using MicPilot.App.ViewModels;
using MicPilot.App.Views.Dialogs;
using WpfButton = System.Windows.Controls.Button;

namespace MicPilot.App.Views.Pages;

public partial class GamesAppsView
{
    public GamesAppsView(ProfilesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public event Action? Saved;

    private ProfilesViewModel Vm => (ProfilesViewModel)DataContext;

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ProfileEditorDialog
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true)
        {
            Vm.AddProfile(dialog.ToProfile());
            Persist();
        }
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as WpfButton)?.Tag is not ProfileItemViewModel item)
        {
            return;
        }

        Vm.SelectedProfile = item;
        var dialog = new ProfileEditorDialog(item)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true)
        {
            dialog.ApplyTo(item);
            Persist();
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as WpfButton)?.Tag is not ProfileItemViewModel item)
        {
            return;
        }

        Vm.SelectedProfile = item;
        Vm.DeleteSelected();
        Persist();
    }

    private void Save_Click(object sender, RoutedEventArgs e) => Persist();

    private void Persist()
    {
        Vm.Save();
        Saved?.Invoke();
    }
}
