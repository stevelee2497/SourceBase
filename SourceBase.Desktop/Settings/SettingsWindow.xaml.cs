using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using SourceBase.Desktop.Models;
using SourceBase.Desktop.Services;

namespace SourceBase.Desktop.Settings;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private bool _showCredentials;
    private ModifierKeys _hotkeyModifiers;
    private Key? _hotkeyKey;

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

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

    public bool Saved { get; private set; }

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        PopulateHours();
        PopulateInterval();
        PopulateDays();
        PauseDuringVideoBox.IsChecked = _settings.PauseDuringVideo;
        StartAtLoginBox.IsChecked = StartupService.IsEnabled();
        _hotkeyModifiers = _settings.HotkeyModifiers;
        _hotkeyKey = _settings.HotkeyKey;
        UpdateHotkeyLabel();
        HotkeyBox.PreviewKeyDown += OnHotkeyPreviewKeyDown;
        ApiUrlBox.Text = _settings.ApiBaseUrl ?? string.Empty;
        UsernameBox.Text = _settings.ApiUsername ?? string.Empty;
        PasswordBox.Password = _settings.ApiPassword ?? string.Empty;

        _showCredentials = string.IsNullOrWhiteSpace(_settings.ApiToken);

        MouseLeftButtonDown += (_, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };
        CancelButton.Click += (_, _) => Close();
        SaveButton.Click += OnSave;
        TestButton.Click += OnTestConnection;

        VersionLabel.Text = $"v{UpdateService.CurrentVersion}";
        ApplyStatusIndicator();
        ApplyApiUiState();

        UpdateButton.Click += OnUpdateClicked;
        CheckUpdateButton.Click += OnCheckUpdate;

        if (UpdateService.PendingUpdate is { } update)
        {
            UpdateBanner.Visibility = Visibility.Visible;
            UpdateAvailableLabel.Text = $"Update available: v{update.Version}";
        }
    }

    // ── API connection status indicator ──────────────────────────────────────

    private void ApplyStatusIndicator()
    {
        var hasToken = !string.IsNullOrWhiteSpace(_settings.ApiToken);
        var hasUrl = !string.IsNullOrWhiteSpace(_settings.ApiBaseUrl);

        if (!hasToken || !hasUrl || _showCredentials) { ApiStatusPanel.Visibility = Visibility.Collapsed; return; }

        ApiStatusPanel.Visibility = Visibility.Visible;

        if (_settings.ApiStatus == ApiConnectionStatus.Failed)
        {
            StatusDot.Fill = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
            StatusText.Text = "Reconnect";
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
            StatusText.MouseLeftButtonUp += OnReconnectClicked;
        }
        else
        {
            StatusDot.Fill = new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A));
            StatusText.Text = "Connected";
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A));
        }
    }

    private void OnReconnectClicked(object sender, MouseButtonEventArgs e)
    {
        _showCredentials = true;
        ApiStatusPanel.Visibility = Visibility.Collapsed;
        ApplyApiUiState();
    }

    // ── Credentials panel visibility ─────────────────────────────────────────

    private void ApplyApiUiState()
    {
        ApiUrlRow.Visibility = _showCredentials ? Visibility.Visible : Visibility.Collapsed;
        CredentialsPanel.Visibility = _showCredentials ? Visibility.Visible : Visibility.Collapsed;
        ApiNoCredentialSpacer.Visibility = _showCredentials ? Visibility.Collapsed : Visibility.Visible;
        TestButton.Visibility = _showCredentials ? Visibility.Visible : Visibility.Collapsed;
        TestResultText.Visibility = Visibility.Collapsed;
    }

    // ── Save ─────────────────────────────────────────────────────────────────

    private async void OnSave(object sender, RoutedEventArgs e)
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
            MessageBox.Show("Pick at least one day.", "Schedule", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_hotkeyKey is not null && _hotkeyModifiers == ModifierKeys.None)
        {
            MessageBox.Show("The rest hotkey must include at least one modifier (Ctrl, Alt, Shift or Win).",
                "Rest Hotkey", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_showCredentials)
        {
            var url = NullIfEmpty(ApiUrlBox.Text);
            var username = NullIfEmpty(UsernameBox.Text);
            var password = NullIfEmpty(PasswordBox.Password);

            if (url is not null && username is not null && password is not null)
            {
                SaveButton.IsEnabled = false;
                var (token, refreshToken, error) = await LoginAsync(url, username, password);
                SaveButton.IsEnabled = true;

                if (error is not null)
                {
                    MessageBox.Show(error, "Login Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _settings.ApiToken = token;
                _settings.ApiRefreshToken = refreshToken;
                _settings.ApiStatus = ApiConnectionStatus.Connected;
            }
            else
            {
                _settings.ApiToken = null;
                _settings.ApiRefreshToken = null;
                _settings.ApiStatus = ApiConnectionStatus.None;
            }

            _settings.ApiBaseUrl = url;
            _settings.ApiUsername = username;
            _settings.ApiPassword = password;
        }
        else
        {
            var newUrl = NullIfEmpty(ApiUrlBox.Text);
            if (newUrl != _settings.ApiBaseUrl)
            {
                _settings.ApiBaseUrl = newUrl;
                _settings.ApiToken = null;
                _settings.ApiRefreshToken = null;
                _settings.ApiStatus = ApiConnectionStatus.None;
            }
        }

        _settings.WorkingHourStart = start;
        _settings.WorkingHourEnd = end;
        _settings.IntervalMinutes = interval;
        _settings.ActiveDays = days;
        _settings.PauseDuringVideo = PauseDuringVideoBox.IsChecked == true;
        _settings.StartAtLogin = StartAtLoginBox.IsChecked == true;
        StartupService.Apply(_settings.StartAtLogin);
        _settings.HotkeyModifiers = _hotkeyModifiers;
        _settings.HotkeyKey = _hotkeyKey;

        Saved = true;
        Close();
    }

    // ── Test Connection ───────────────────────────────────────────────────────

    private async void OnTestConnection(object sender, RoutedEventArgs e)
    {
        var url = NullIfEmpty(ApiUrlBox.Text);
        var username = NullIfEmpty(UsernameBox.Text);
        var password = NullIfEmpty(PasswordBox.Password);

        if (url is null || username is null || password is null)
        {
            SetTestResult("Enter API URL, username and password first.", success: false);
            return;
        }

        TestButton.IsEnabled = false;
        TestResultText.Text = "Testing…";
        TestResultText.Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));
        TestResultText.Visibility = Visibility.Visible;

        var (_, _, error) = await LoginAsync(url, username, password);

        TestButton.IsEnabled = true;
        if (error is null) SetTestResult("Connected ✓", success: true);
        else SetTestResult(error, success: false);
    }

    private void SetTestResult(string text, bool success)
    {
        TestResultText.Text = text;
        TestResultText.Foreground = new SolidColorBrush(success
            ? Color.FromRgb(0x16, 0xA3, 0x4A)
            : Color.FromRgb(0xDC, 0x26, 0x26));
        TestResultText.Visibility = Visibility.Visible;
    }

    // ── OTA update ────────────────────────────────────────────────────────────

    private async void OnCheckUpdate(object sender, RoutedEventArgs e)
    {
        CheckUpdateButton.IsEnabled = false;
        RefreshIcon.Text = "…";
        await UpdateService.CheckAsync();
        CheckUpdateButton.IsEnabled = true;

        if (UpdateService.PendingUpdate is { } update)
        {
            UpdateBanner.Visibility = Visibility.Visible;
            UpdateAvailableLabel.Text = $"Update available: v{update.Version}";
            RefreshIcon.Text = "↻";
        }
        else
        {
            RefreshIcon.Text = "✓";
            RefreshIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A));
            await Task.Delay(2000);
            RefreshIcon.Text = "↻";
            RefreshIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF));
        }
    }

    private async void OnUpdateClicked(object sender, RoutedEventArgs e)
    {
        UpdateButton.IsEnabled = false;
        UpdateButton.Content = "Downloading…";
        try
        {
            await UpdateService.ApplyUpdateAsync(pct =>
                Dispatcher.Invoke(() => UpdateButton.Content = $"{pct}%"));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Update failed: {ex.Message}", "Update Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            UpdateButton.IsEnabled = true;
            UpdateButton.Content = "Update";
        }
    }

    // ── Rest hotkey ───────────────────────────────────────────────────────────

    private void OnHotkeyPreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key == Key.Escape) { _hotkeyModifiers = ModifierKeys.None; _hotkeyKey = null; UpdateHotkeyLabel(); return; }

        var isModifierOnly = key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin;
        if (isModifierOnly) return;

        _hotkeyModifiers = Keyboard.Modifiers;
        _hotkeyKey = key;
        UpdateHotkeyLabel();
    }

    private void UpdateHotkeyLabel() =>
        HotkeyBox.Text = _hotkeyKey is null ? "Disabled — click and press a combo to enable" : FormatHotkey(_hotkeyModifiers, _hotkeyKey.Value);

    private static string FormatHotkey(ModifierKeys mods, Key key)
    {
        var parts = new List<string>();
        if (mods.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (mods.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (mods.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (mods.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(key.ToString());
        return string.Join(" + ", parts);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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
        IntervalBox.SelectedIndex = match >= 0 ? match : 1;
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

    private async Task<(string? token, string? refreshToken, string? error)> LoginAsync(string url, string username, string password)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new { email = username, password }, JsonOpts);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{url.TrimEnd('/')}/api/auth/login")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            };
            var resp = await Http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
                return (null, null, $"Login failed ({(int)resp.StatusCode})");

            var body = await JsonSerializer.DeserializeAsync<LoginResponse>(
                await resp.Content.ReadAsStreamAsync(), JsonOpts);
            if (body is null || string.IsNullOrWhiteSpace(body.AccessToken))
                return (null, null, "Invalid response from server");

            return (body.AccessToken, body.RefreshToken, null);
        }
        catch (TaskCanceledException) { return (null, null, "Connection timed out"); }
        catch (Exception ex) { return (null, null, ex.Message); }
    }

    private static int SelectedHour(ComboBox box) => (int)((ComboBoxItem)box.SelectedItem).Tag!;
    private static T SelectedTag<T>(ComboBox box) => (T)((ComboBoxItem)box.SelectedItem).Tag!;
    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string FormatHour(int h)
    {
        var period = h < 12 ? "AM" : "PM";
        var display = h % 12 == 0 ? 12 : h % 12;
        return $"{display}:00 {period}";
    }

    private record LoginResponse(string AccessToken, string RefreshToken);
}
