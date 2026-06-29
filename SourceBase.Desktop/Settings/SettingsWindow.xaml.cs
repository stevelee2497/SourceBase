using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using SourceBase.Desktop.Models;

namespace SourceBase.Desktop.Settings;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    // Interval options shown in the dropdown, label → minutes.
    private static readonly (string Label, int Minutes)[] Intervals =
    [
        ("15 minutes", 15),
        ("30 minutes", 30),
        ("45 minutes", 45),
        ("1 hour", 60),
        ("1.5 hours", 90),
        ("2 hours", 120),
    ];

    private static readonly (string Label, DayOfWeek Day)[] Days =
    [
        ("Mon", DayOfWeek.Monday),
        ("Tue", DayOfWeek.Tuesday),
        ("Wed", DayOfWeek.Wednesday),
        ("Thu", DayOfWeek.Thursday),
        ("Fri", DayOfWeek.Friday),
        ("Sat", DayOfWeek.Saturday),
        ("Sun", DayOfWeek.Sunday),
    ];

    /// <summary>True when the user pressed Save (so the caller can persist + reschedule).</summary>
    public bool Saved { get; private set; }

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        PopulateHours();
        PopulateInterval();
        PopulateDays();
        PauseDuringVideoBox.IsChecked = _settings.PauseDuringVideo;

        MouseLeftButtonDown += (_, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };
        CancelButton.Click += (_, _) => Close();
        SaveButton.Click += OnSave;
    }

    private void PopulateHours()
    {
        for (var h = 0; h < 24; h++)
        {
            var label = FormatHour(h);
            StartHour.Items.Add(new ComboBoxItem { Content = label, Tag = h });
            EndHour.Items.Add(new ComboBoxItem { Content = label, Tag = h });
        }
        StartHour.SelectedIndex = _settings.WorkingHourStart ?? 9;
        EndHour.SelectedIndex = _settings.WorkingHourEnd ?? 17;
    }

    private void PopulateInterval()
    {
        foreach (var (label, minutes) in Intervals)
            IntervalBox.Items.Add(new ComboBoxItem { Content = label, Tag = minutes });

        var match = Array.FindIndex(Intervals, i => i.Minutes == _settings.IntervalMinutes);
        IntervalBox.SelectedIndex = match >= 0 ? match : 1; // default to 30 min
    }

    private void PopulateDays()
    {
        foreach (var (label, day) in Days)
        {
            var pill = new ToggleButton
            {
                Content = label,
                Style = (Style)Resources["DayPill"],
                IsChecked = _settings.ActiveDays.Contains(day),
                Tag = day,
            };
            DayRow.Children.Add(pill);
        }
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var start = SelectedHour(StartHour);
        var end = SelectedHour(EndHour);
        var interval = SelectedTag<int>(IntervalBox);

        var days = DayRow.Children.OfType<ToggleButton>()
            .Where(p => p.IsChecked == true)
            .Select(p => (DayOfWeek)p.Tag!)
            .ToList();

        if (days.Count == 0)
        {
            MessageBox.Show("Pick at least one day.", "Schedule",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _settings.WorkingHourStart = start;
        _settings.WorkingHourEnd = end;
        _settings.IntervalMinutes = interval;
        _settings.ActiveDays = days;
        _settings.PauseDuringVideo = PauseDuringVideoBox.IsChecked == true;

        Saved = true;
        Close();
    }

    private static int SelectedHour(ComboBox box) => (int)((ComboBoxItem)box.SelectedItem).Tag!;
    private static T SelectedTag<T>(ComboBox box) => (T)((ComboBoxItem)box.SelectedItem).Tag!;

    private static string FormatHour(int h)
    {
        var period = h < 12 ? "AM" : "PM";
        var display = h % 12 == 0 ? 12 : h % 12;
        return $"{display}:00 {period}";
    }
}
