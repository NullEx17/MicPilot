using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace MicPilot.App.Views.Pages;

public partial class AboutView
{
    public AboutView()
    {
        InitializeComponent();
    }

    private void Website_Click(object sender, RoutedEventArgs e) =>
        OpenUrl("https://nullex17.me");

    private void GitHub_Click(object sender, RoutedEventArgs e) =>
        OpenUrl("https://github.com/NullEx17");

    private void Discord_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Clipboard.SetText("@NullEx17");
        DiscordHandle.Text = "copied @NullEx17";
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.6) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            DiscordHandle.Text = "@NullEx17";
        };
        timer.Start();
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
