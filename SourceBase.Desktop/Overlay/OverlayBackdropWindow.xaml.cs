using System.Windows;
using System.Windows.Media.Animation;

namespace SourceBase.Desktop.Overlay;

/// <summary>
/// Dims a non-primary monitor while <see cref="OverlayWindow"/> is showing on the primary one,
/// so a reminder can't be missed just because the user is looking at a different screen.
/// Carries no interactive content — <see cref="OverlayWindow"/> stays the single source of truth
/// for snooze/dismiss/start decisions.
/// </summary>
public partial class OverlayBackdropWindow : Window
{
    public OverlayBackdropWindow(MonitorHelper.MonitorBounds bounds)
    {
        InitializeComponent();

        Left = bounds.Left;
        Top = bounds.Top;
        Width = bounds.Width;
        Height = bounds.Height;

        Opacity = 0;
        Loaded += (_, _) => BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
    }
}
