using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MicPilot.Core.Models;
using MicPilot.Diagnostics;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
using Pen = System.Drawing.Pen;
using SolidBrush = System.Drawing.SolidBrush;

namespace MicPilot.App.Tray;

public sealed class TrayIconService : IDisposable
{
    private static readonly Color MenuBack = Color.FromArgb(21, 23, 27);
    private static readonly Color MenuBorder = Color.FromArgb(41, 44, 50);
    private static readonly Color MenuText = Color.FromArgb(245, 245, 245);
    private static readonly Color MenuMuted = Color.FromArgb(155, 155, 160);
    private static readonly Color MenuHover = Color.FromArgb(34, 37, 43);
    private static readonly Color StatusOn = Color.FromArgb(50, 220, 95);
    private static readonly Color StatusOff = Color.FromArgb(235, 55, 60);

    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _iconOn;
    private readonly Icon _iconOff;
    private readonly Icon _iconIdle;
    private readonly ToolStripMenuItem _headerItem;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _toggleItem;
    private readonly PrivateFontCollection _fonts = new();
    private IntPtr _fontMemory = IntPtr.Zero;
    private readonly Font _menuFont;
    private bool _disposed;
    private bool _isOn = true;
    private bool _audioRunning;

    public TrayIconService()
    {
        // Use the app icons (dark rounded tile + mic) so tray matches the window icon.
        _iconOn = LoadAppTrayIcon("micpilot_on_32x32.png");
        _iconOff = LoadAppTrayIcon("micpilot_muted_32x32.png");
        _iconIdle = LoadAppTrayIcon("micpilot_idle_32x32.png");
        _menuFont = CreateMontserratFont(9f);

        _notifyIcon = new NotifyIcon
        {
            Icon = _iconIdle,
            Text = "MicPilot",
            Visible = true
        };

        _notifyIcon.DoubleClick += (_, _) => OpenRequested?.Invoke();

        _headerItem = new ToolStripMenuItem("MicPilot") { Enabled = false };
        _statusItem = new ToolStripMenuItem("Game Mic: —") { Enabled = false };
        _toggleItem = new ToolStripMenuItem("Toggle Game Mic", null, (_, _) => ToggleRequested?.Invoke());

        var menu = new ContextMenuStrip
        {
            Renderer = new DarkTrayRenderer(),
            BackColor = MenuBack,
            ForeColor = MenuText,
            ShowImageMargin = false,
            ShowCheckMargin = false,
            Padding = new Padding(6, 6, 6, 6),
            Font = _menuFont
        };

        menu.Items.Add(_headerItem);
        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_toggleItem);
        menu.Items.Add(new ToolStripMenuItem("Games & Apps", null, (_, _) => ProfilesRequested?.Invoke()));
        menu.Items.Add(new ToolStripMenuItem("Settings", null, (_, _) => SettingsRequested?.Invoke()));
        menu.Items.Add(new ToolStripMenuItem("Open MicPilot", null, (_, _) => OpenRequested?.Invoke()));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => ExitRequested?.Invoke()));
        _notifyIcon.ContextMenuStrip = menu;

        Log.Info("System tray icon created (NullEx17 brand assets)");
    }

    public event Action? ToggleRequested;
    public event Action? OpenRequested;
    public event Action? ProfilesRequested;
    public event Action? SettingsRequested;
    public event Action? ExitRequested;

    public void UpdateState(GameMicState state, string profileName, bool audioEngineRunning, string hotkey)
    {
        _audioRunning = audioEngineRunning;
        _isOn = state == GameMicState.On;

        if (!audioEngineRunning)
        {
            _notifyIcon.Icon = _iconIdle;
            _statusItem.Text = "Game Mic: —";
            _notifyIcon.Text = Truncate("MicPilot — audio unavailable");
        }
        else
        {
            _notifyIcon.Icon = _isOn ? _iconOn : _iconOff;
            var micLabel = _isOn ? "ON" : "MUTED";
            _statusItem.Text = $"Game Mic: {micLabel}";
            _notifyIcon.Text = Truncate($"MicPilot — {profileName}: {micLabel}");
        }

        _toggleItem.Text = string.IsNullOrWhiteSpace(hotkey)
            ? "Toggle Game Mic"
            : $"Toggle Game Mic        {hotkey}";

        _statusItem.ForeColor = !_audioRunning
            ? MenuMuted
            : _isOn ? StatusOn : StatusOff;
    }

    public void ShowNotification(string title, string message, bool enabled)
    {
        if (!enabled)
        {
            return;
        }

        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(1800);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _iconOn.Dispose();
        _iconOff.Dispose();
        _iconIdle.Dispose();
        _menuFont.Dispose();
        _fonts.Dispose();
        if (_fontMemory != IntPtr.Zero)
        {
            Marshal.FreeCoTaskMem(_fontMemory);
            _fontMemory = IntPtr.Zero;
        }
    }

    private static string Truncate(string text) =>
        text.Length <= 63 ? text : text[..60] + "...";

    private Font CreateMontserratFont(float size)
    {
        try
        {
            var uri = new Uri("pack://application:,,,/MicPilot;component/Assets/fonts/Montserrat-Regular.ttf", UriKind.Absolute);
            var resource = System.Windows.Application.GetResourceStream(uri);
            if (resource is null)
            {
                return new Font("Segoe UI", size, FontStyle.Regular);
            }

            using var stream = resource.Stream;
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            var bytes = ms.ToArray();
            _fontMemory = Marshal.AllocCoTaskMem(bytes.Length);
            Marshal.Copy(bytes, 0, _fontMemory, bytes.Length);
            _fonts.AddMemoryFont(_fontMemory, bytes.Length);
            return new Font(_fonts.Families[0], size, FontStyle.Regular);
        }
        catch
        {
            return new Font("Segoe UI", size, FontStyle.Regular);
        }
    }

    private static Icon LoadAppTrayIcon(string fileName)
    {
        var uri = new Uri($"pack://application:,,,/MicPilot;component/Assets/app/{fileName}", UriKind.Absolute);
        var resource = System.Windows.Application.GetResourceStream(uri)
                       ?? throw new FileNotFoundException($"Missing app icon: {fileName}");

        using var stream = resource.Stream;
        using var source = new Bitmap(stream);
        var size = Math.Max(32, SystemInformation.SmallIconSize.Width);

        using var canvas = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(canvas))
        {
            graphics.Clear(Color.Transparent);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.DrawImage(source, new Rectangle(0, 0, size, size));
        }

        var handle = canvas.GetHicon();
        var icon = Icon.FromHandle(handle);
        var clone = (Icon)icon.Clone();
        NativeMethods.DestroyIcon(handle);
        return clone;
    }

    private sealed class DarkTrayRenderer : ToolStripProfessionalRenderer
    {
        public DarkTrayRenderer() : base(new DarkColorTable())
        {
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var brush = new SolidBrush(MenuBack);
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using var pen = new Pen(MenuBorder);
            var bounds = e.AffectedBounds;
            bounds.Width -= 1;
            bounds.Height -= 1;
            e.Graphics.DrawRectangle(pen, bounds);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected || !e.Item.Enabled)
            {
                return;
            }

            var rect = new Rectangle(4, 0, e.Item.Width - 8, e.Item.Height);
            using var brush = new SolidBrush(MenuHover);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = Rounded(rect, 4);
            e.Graphics.FillPath(brush, path);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (!e.Item.Enabled)
            {
                e.TextColor = e.Item.ForeColor == MenuText ? MenuMuted : e.Item.ForeColor;
            }
            else if (e.Item.Selected)
            {
                e.TextColor = MenuText;
            }

            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var y = e.Item.Height / 2;
            using var pen = new Pen(MenuBorder);
            e.Graphics.DrawLine(pen, 10, y, e.Item.Width - 10, y);
        }

        private static GraphicsPath Rounded(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            var d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    private sealed class DarkColorTable : ProfessionalColorTable
    {
        public override Color MenuBorder => MenuBorderColor;
        public override Color MenuStripGradientBegin => MenuBack;
        public override Color MenuStripGradientEnd => MenuBack;
        public override Color ToolStripDropDownBackground => MenuBack;
        public override Color ImageMarginGradientBegin => MenuBack;
        public override Color ImageMarginGradientMiddle => MenuBack;
        public override Color ImageMarginGradientEnd => MenuBack;
        public override Color SeparatorDark => MenuBorderColor;
        public override Color SeparatorLight => MenuBorderColor;

        private static Color MenuBorderColor => Color.FromArgb(41, 44, 50);
    }

    private static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool DestroyIcon(IntPtr handle);
    }
}
