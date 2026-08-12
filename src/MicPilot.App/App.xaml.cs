using System.Runtime.InteropServices;
using System.Threading;
using MicPilot.Diagnostics;

namespace MicPilot.App;

public partial class App : System.Windows.Application
{
    private const string MutexName = @"Local\MicPilot.NullEx17.SingleInstance";
    private Mutex? _singleInstanceMutex;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        _singleInstanceMutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            ActivateExistingInstance();
            Shutdown();
            return;
        }

        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error($"Unhandled UI exception: {args.Exception}");
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Log.Error($"Unhandled exception: {args.ExceptionObject}");

        base.OnStartup(e);
        Log.Info("MicPilot starting");
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        Log.Info("MicPilot exiting");
        try
        {
            _singleInstanceMutex?.ReleaseMutex();
        }
        catch
        {
            // ignored
        }

        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private static void ActivateExistingInstance()
    {
        var hwnd = NativeMethods.FindWindow(null, "MicPilot");
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.ShowWindow(hwnd, NativeMethods.SwRestore);
        NativeMethods.SetForegroundWindow(hwnd);
    }

    private static class NativeMethods
    {
        public const int SwRestore = 9;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
