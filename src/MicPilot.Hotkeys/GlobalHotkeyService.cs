using System.Runtime.InteropServices;
using System.Windows.Interop;
using MicPilot.Diagnostics;

namespace MicPilot.Hotkeys;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 0x4D50; // 'MP'

    private readonly object _sync = new();
    private HwndSource? _source;
    private HotkeyDefinition? _registered;
    private bool _disposed;

    public event Action? Pressed;

    public bool IsRegistered => _registered is not null;

    public HotkeyDefinition? Current => _registered;

    public bool Attach(IntPtr windowHandle)
    {
        lock (_sync)
        {
            DetachInternal();

            if (windowHandle == IntPtr.Zero)
            {
                return false;
            }

            _source = HwndSource.FromHwnd(windowHandle);
            if (_source is null)
            {
                Log.Warn("Could not attach hotkey service to window handle.");
                return false;
            }

            _source.AddHook(WndProc);
            return true;
        }
    }

    public bool Register(HotkeyDefinition definition)
    {
        lock (_sync)
        {
            if (_source?.Handle is not IntPtr hwnd || hwnd == IntPtr.Zero)
            {
                Log.Warn("Hotkey registration skipped: window not ready.");
                return false;
            }

            UnregisterInternal(hwnd);

            // MOD_NOREPEAT avoids auto-repeat spam for toggle mode.
            const uint modNoRepeat = 0x4000;
            var modifiers = definition.Modifiers | modNoRepeat;

            if (!NativeMethods.RegisterHotKey(hwnd, HotkeyId, modifiers, definition.VirtualKey))
            {
                var error = Marshal.GetLastWin32Error();
                Log.Warn($"Failed to register hotkey '{definition.DisplayName}' (Win32 {error}).");
                _registered = null;
                return false;
            }

            _registered = definition;
            Log.Info($"Registered global hotkey: {definition.DisplayName}");
            return true;
        }
    }

    public void Unregister()
    {
        lock (_sync)
        {
            if (_source?.Handle is IntPtr hwnd && hwnd != IntPtr.Zero)
            {
                UnregisterInternal(hwnd);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        lock (_sync)
        {
            DetachInternal();
        }
    }

    private void DetachInternal()
    {
        if (_source is not null)
        {
            if (_source.Handle != IntPtr.Zero)
            {
                UnregisterInternal(_source.Handle);
            }

            _source.RemoveHook(WndProc);
            _source = null;
        }

        _registered = null;
    }

    private void UnregisterInternal(IntPtr hwnd)
    {
        if (_registered is null)
        {
            return;
        }

        NativeMethods.UnregisterHotKey(hwnd, HotkeyId);
        Log.Info($"Unregistered global hotkey: {_registered.DisplayName}");
        _registered = null;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            Pressed?.Invoke();
        }

        return IntPtr.Zero;
    }
}
