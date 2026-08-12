using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MicPilot.Core.Models;
using MediaBrush = System.Windows.Media.Brush;

namespace MicPilot.App.Overlay;

public partial class StatusOverlayWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;
    private const int WsExLayered = 0x00080000;

    private readonly DispatcherTimer _hideTimer = new();
    private readonly BitmapImage _iconOn;
    private readonly BitmapImage _iconMuted;

    public StatusOverlayWindow()
    {
        InitializeComponent();
        _iconOn = LoadOverlayBitmap("micpilot_overlay_on_32x32.png");
        _iconMuted = LoadOverlayBitmap("micpilot_overlay_muted_32x32.png");

        _hideTimer.Tick += (_, _) => FadeOut();
        SourceInitialized += (_, _) => ApplyClickThroughStyles();
        Loaded += (_, _) => ApplyClickThroughStyles();
    }

    public void ShowState(GameMicState state, AppSettings settings)
    {
        if (!settings.OverlayEnabled)
        {
            HideImmediate();
            return;
        }

        Opacity = Math.Clamp(settings.OverlayOpacity, 0.4, 1.0);
        ApplyVisual(state, settings.OverlaySize);
        PositionOnScreen(settings.OverlayPosition);
        ShowWithoutActivation();
        FadeIn();

        _hideTimer.Stop();
        if (!settings.OverlayAlwaysVisible && settings.OverlayShowOnChange)
        {
            var seconds = Math.Clamp(settings.OverlayDurationSeconds, 0.5, 10);
            _hideTimer.Interval = TimeSpan.FromSeconds(seconds);
            _hideTimer.Start();
        }
    }

    public void RefreshAlwaysVisible(GameMicState state, AppSettings settings)
    {
        if (!settings.OverlayEnabled || !settings.OverlayAlwaysVisible)
        {
            return;
        }

        Opacity = Math.Clamp(settings.OverlayOpacity, 0.4, 1.0);
        ApplyVisual(state, settings.OverlaySize);
        PositionOnScreen(settings.OverlayPosition);
        ShowWithoutActivation();
        Root.Opacity = 1;
    }

    public void HideImmediate()
    {
        _hideTimer.Stop();
        Root.BeginAnimation(UIElement.OpacityProperty, null);
        Root.Opacity = 0;
        Hide();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ApplyClickThroughStyles();
    }

    private void ApplyVisual(GameMicState state, OverlaySize size)
    {
        var muted = state == GameMicState.Off;
        StatusLabel.Text = muted ? "MUTED" : "ON";
        StatusLabel.Foreground = muted
            ? (MediaBrush)(System.Windows.Application.Current.TryFindResource("StatusOffBrush")
               ?? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xEB, 0x37, 0x3C)))
            : (MediaBrush)(System.Windows.Application.Current.TryFindResource("StatusOnBrush")
               ?? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x32, 0xDC, 0x5F)));
        StateDot.Fill = StatusLabel.Foreground;
        StateIcon.Source = muted ? _iconMuted : _iconOn;

        var scale = size == OverlaySize.Medium ? 1.15 : 1.0;
        Root.LayoutTransform = new ScaleTransform(scale, scale);
    }

    private void PositionOnScreen(OverlayPosition position)
    {
        var work = SystemParameters.WorkArea;
        var margin = 22 * (DpiX / 96.0);

        UpdateLayout();
        var width = ActualWidth > 0 ? ActualWidth : 120;
        var height = ActualHeight > 0 ? ActualHeight : 56;

        Left = position switch
        {
            OverlayPosition.TopLeft or OverlayPosition.BottomLeft => work.Left + margin,
            _ => work.Right - width - margin
        };

        Top = position switch
        {
            OverlayPosition.BottomLeft or OverlayPosition.BottomRight => work.Bottom - height - margin,
            _ => work.Top + margin
        };
    }

    private double DpiX
    {
        get
        {
            var source = PresentationSource.FromVisual(this);
            return source?.CompositionTarget?.TransformToDevice.M11 * 96.0 ?? 96.0;
        }
    }

    private void ShowWithoutActivation()
    {
        if (!IsVisible)
        {
            Show();
        }

        ApplyClickThroughStyles();
    }

    private void FadeIn()
    {
        Root.BeginAnimation(UIElement.OpacityProperty, null);
        var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(120))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        Root.BeginAnimation(UIElement.OpacityProperty, anim);
    }

    private void FadeOut()
    {
        var anim = new DoubleAnimation(Root.Opacity, 0, TimeSpan.FromMilliseconds(180));
        anim.Completed += (_, _) => Hide();
        Root.BeginAnimation(UIElement.OpacityProperty, anim);
    }

    private void ApplyClickThroughStyles()
    {
        var helper = new WindowInteropHelper(this);
        if (helper.Handle == IntPtr.Zero)
        {
            return;
        }

        var ex = NativeMethods.GetWindowLong(helper.Handle, GwlExStyle);
        ex |= WsExTransparent | WsExToolWindow | WsExNoActivate | WsExLayered;
        NativeMethods.SetWindowLong(helper.Handle, GwlExStyle, ex);
    }

    private static BitmapImage LoadOverlayBitmap(string fileName)
    {
        var uri = new Uri($"pack://application:,,,/MicPilot;component/Assets/overlay/{fileName}", UriKind.Absolute);
        var resource = System.Windows.Application.GetResourceStream(uri)
                       ?? throw new FileNotFoundException($"Missing overlay asset: {fileName}");

        var image = new BitmapImage();
        image.BeginInit();
        image.StreamSource = resource.Stream;
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }
}
