using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SourceBase.Desktop.Models;

namespace SourceBase.Desktop.Overlay;

public partial class OverlayWindow : Window
{
    private readonly AppSettings _settings;
    private readonly DispatcherTimer _restTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private TimeSpan _remaining;
    private bool _restStarted;

    public event EventHandler? Snoozed;
    public event EventHandler<Habit>? HabitPicked;

    public OverlayWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        SubtitleText.Text = $"Step away for {settings.RestMinutes} minutes. Pick one to do now.";
        SnoozeButton.Content = $"Snooze {settings.SnoozeMinutes} min";

        CoverPrimaryScreen();
        BuildCards();

        SnoozeButton.Click += (_, _) => { Snoozed?.Invoke(this, EventArgs.Empty); Close(); };
        DismissButton.Click += (_, _) => Close();
        KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Escape) Close(); };

        _restTimer.Tick += OnRestTick;
        Loaded += (_, _) => FadeIn();
    }

    private void CoverPrimaryScreen()
    {
        // Cover the primary working area. Multi-monitor: spawn one window per screen
        // from the caller using SystemParameters / per-screen bounds.
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;
        WindowState = WindowState.Normal;
    }

    private void FadeIn()
    {
        Opacity = 0;
        BeginAnimation(OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var scale = new DoubleAnimation(0.96, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = ease };
        ModalScale.BeginAnimation(ScaleTransform.ScaleXProperty, scale);
        ModalScale.BeginAnimation(ScaleTransform.ScaleYProperty, scale);
    }

    private void BuildCards()
    {
        foreach (var habit in _settings.Habits.Where(h => h.IsEnabled))
            HabitItems.Items.Add(BuildCard(habit));
    }

    private Border BuildCard(Habit habit)
    {
        var accent = ParseColor(habit.Accent) ?? Color.FromRgb(0x37, 0x41, 0x51);

        var visual = new ContentControl
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Content = BuildVisual(habit),
            Margin = new Thickness(0, 0, 0, 10),
        };

        var label = new TextBlock
        {
            Text = habit.Name,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x29, 0x37)),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
        };

        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(visual);
        stack.Children.Add(label);

        var card = new Border
        {
            Width = 140,
            Height = 140,
            Margin = new Thickness(8),
            CornerRadius = new CornerRadius(16),
            Background = new SolidColorBrush(Color.FromRgb(0xF9, 0xFA, 0xFB)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xEC, 0xEE, 0xF1)),
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand,
            Child = stack,
            Tag = habit,
        };

        // Hover: tint border + lift with the accent color.
        card.MouseEnter += (_, _) =>
        {
            card.BorderBrush = new SolidColorBrush(accent);
            card.Background = new SolidColorBrush(Color.FromArgb(0x14, accent.R, accent.G, accent.B));
        };
        card.MouseLeave += (_, _) =>
        {
            card.BorderBrush = new SolidColorBrush(Color.FromRgb(0xEC, 0xEE, 0xF1));
            card.Background = new SolidColorBrush(Color.FromRgb(0xF9, 0xFA, 0xFB));
        };
        card.MouseLeftButtonUp += (_, _) => OnHabitPicked(habit);

        return card;
    }

    private UIElement BuildVisual(Habit habit)
    {
        // Prefer an image if one is configured and exists; otherwise the emoji.
        if (!string.IsNullOrWhiteSpace(habit.ImagePath) && File.Exists(habit.ImagePath))
        {
            return new Image
            {
                Width = 56,
                Height = 56,
                Source = new BitmapImage(new Uri(habit.ImagePath)),
                Stretch = Stretch.Uniform,
            };
        }

        return new TextBlock
        {
            Text = habit.Emoji ?? "✅",
            FontSize = 44,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
    }

    private void OnHabitPicked(Habit habit)
    {
        HabitPicked?.Invoke(this, habit);

        // Switch the modal into "resting" mode with a countdown.
        _restStarted = true;
        HabitItems.Visibility = Visibility.Collapsed;
        SnoozeButton.Visibility = Visibility.Collapsed;
        DismissButton.Content = "I'm done";
        SubtitleText.Text = $"{habit.Emoji} {habit.Name} — nice. Take your time.";

        _remaining = TimeSpan.FromMinutes(_settings.RestMinutes);
        UpdateCountdown();
        _restTimer.Start();
    }

    private void OnRestTick(object? sender, EventArgs e)
    {
        _remaining = _remaining.Subtract(TimeSpan.FromSeconds(1));
        if (_remaining <= TimeSpan.Zero)
        {
            _restTimer.Stop();
            Close();
            return;
        }
        UpdateCountdown();
    }

    private void UpdateCountdown() =>
        CountdownText.Text = _restStarted
            ? $"{_remaining:m\\:ss} remaining"
            : string.Empty;

    private static Color? ParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        try { return (Color)ColorConverter.ConvertFromString(hex); }
        catch { return null; }
    }
}
