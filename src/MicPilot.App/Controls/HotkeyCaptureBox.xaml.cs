using System.Windows;
using System.Windows.Input;
using MicPilot.Hotkeys;

namespace MicPilot.App.Controls;

public partial class HotkeyCaptureBox : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty HotkeyProperty =
        DependencyProperty.Register(
            nameof(Hotkey),
            typeof(string),
            typeof(HotkeyCaptureBox),
            new FrameworkPropertyMetadata("PgDn", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty IsCapturingProperty =
        DependencyProperty.Register(
            nameof(IsCapturing),
            typeof(bool),
            typeof(HotkeyCaptureBox),
            new PropertyMetadata(false, OnIsCapturingChanged));

    public HotkeyCaptureBox()
    {
        InitializeComponent();
        PreviewKeyDown += OnPreviewKeyDown;
        LostKeyboardFocus += (_, _) =>
        {
            if (IsCapturing)
            {
                CancelCapture();
            }
        };
        Loaded += (_, _) => UpdateVisualState();
    }

    public string Hotkey
    {
        get => (string)GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    public bool IsCapturing
    {
        get => (bool)GetValue(IsCapturingProperty);
        set => SetValue(IsCapturingProperty, value);
    }

    public string? ValidationError { get; private set; }

    public event Action<string>? HotkeyChanged;
    public event Action<string>? CaptureFailed;

    private void Change_Click(object sender, RoutedEventArgs e)
    {
        ValidationError = null;
        IsCapturing = true;
        Focusable = true;
        Focus();
        Keyboard.Focus(this);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => CancelCapture();

    private void CancelCapture()
    {
        IsCapturing = false;
        ValidationError = null;
    }

    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!IsCapturing)
        {
            return;
        }

        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin
            or Key.Escape)
        {
            if (key == Key.Escape)
            {
                CancelCapture();
            }

            return;
        }

        var parts = new List<string>();
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            parts.Add("Ctrl");
        }

        if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)
        {
            parts.Add("Alt");
        }

        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
        {
            parts.Add("Shift");
        }

        var keyName = MapKey(key);
        if (keyName is null)
        {
            ValidationError = "That key isn't supported. Try PgDn, Home, F9–F12, or Insert.";
            CaptureFailed?.Invoke(ValidationError);
            return;
        }

        parts.Add(keyName);
        var candidate = string.Join("+", parts);

        if (!HotkeyParser.TryParse(candidate, out var definition))
        {
            ValidationError = "That hotkey isn't valid.";
            CaptureFailed?.Invoke(ValidationError);
            return;
        }

        Hotkey = definition.DisplayName;
        IsCapturing = false;
        HotkeyChanged?.Invoke(Hotkey);
    }

    private static string? MapKey(Key key) => key switch
    {
        Key.PageDown => "PgDn",
        Key.PageUp => "PgUp",
        Key.Home => "Home",
        Key.End => "End",
        Key.Insert => "Insert",
        Key.Delete => "Delete",
        Key.F1 => "F1",
        Key.F2 => "F2",
        Key.F3 => "F3",
        Key.F4 => "F4",
        Key.F5 => "F5",
        Key.F6 => "F6",
        Key.F7 => "F7",
        Key.F8 => "F8",
        Key.F9 => "F9",
        Key.F10 => "F10",
        Key.F11 => "F11",
        Key.F12 => "F12",
        Key.Space => "Space",
        Key.Pause => "Pause",
        _ => null
    };

    private static void OnIsCapturingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HotkeyCaptureBox box)
        {
            box.UpdateVisualState();
        }
    }

    private void UpdateVisualState()
    {
        if (CapturePrompt is null || DisplayText is null)
        {
            return;
        }

        CapturePrompt.Visibility = IsCapturing ? Visibility.Visible : Visibility.Collapsed;
        DisplayText.Visibility = IsCapturing ? Visibility.Collapsed : Visibility.Visible;
        ChangeButton.Visibility = IsCapturing ? Visibility.Collapsed : Visibility.Visible;
        CancelButton.Visibility = IsCapturing ? Visibility.Visible : Visibility.Collapsed;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateVisualState();
    }
}
