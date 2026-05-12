using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace mkAutoClicker;

public partial class MainWindow : Window {
    private const double MinimumClicksPerSecond = 0.1;
    private const double MaximumClicksPerSecond = 1000.0;
    private const double MinimumIntervalMilliseconds = 1.0;
    private const double MaximumIntervalMilliseconds = 10000.0;

    private readonly List<OptionItem<ActionType>> _actionModes;


    private readonly AutoClickerEngine _engine;
    private readonly DispatcherTimer _holdModeTimer;
    private readonly List<OptionItem<HotkeyMode>> _hotkeyModes;
    private readonly GlobalHotkeyService _hotkeyService;
    private readonly List<KeyOption> _keyboardKeys;

    private HotkeyConfig _hotkeyConfig;
    private bool _isApplyingAutoHeight;
    private bool _isCapturingHotkey;

    private bool _isRunning;
    private bool _isSynchronizingRateFields;
    private bool _isWindowReady;
    private DateTime _lastHotkeyPressUtc = DateTime.MinValue;
    private CancellationTokenSource? _runCancellation;

    private HwndSource? _windowSource;

    public MainWindow() {
        InitializeComponent();
        MaxHeight = SystemParameters.WorkArea.Height;

        _engine = new AutoClickerEngine();
        _hotkeyService = new GlobalHotkeyService();
        _hotkeyService.Pressed += HotkeyService_Pressed;

        _holdModeTimer = new DispatcherTimer {
            Interval = TimeSpan.FromMilliseconds(35)
        };
        _holdModeTimer.Tick += HoldModeTimer_Tick;

        _actionModes = new List<OptionItem<ActionType>> {
            new(LocalizationService.GetString("Ui.ActionType.MouseLeft", "Mouse Left"), ActionType.MouseLeft),
            new(LocalizationService.GetString("Ui.ActionType.MouseMiddle", "Mouse Middle"), ActionType.MouseMiddle),
            new(LocalizationService.GetString("Ui.ActionType.MouseRight", "Mouse Right"), ActionType.MouseRight),
            new(LocalizationService.GetString("Ui.ActionType.KeyboardKey", "Keyboard Key"), ActionType.KeyboardKey)
        };
        _hotkeyModes = new List<OptionItem<HotkeyMode>> {
            new(LocalizationService.GetString("Ui.HotkeyMode.Toggle", "Toggle"), HotkeyMode.Toggle),
            new(LocalizationService.GetString("Ui.HotkeyMode.Hold", "Hold"), HotkeyMode.Hold)
        };
        _keyboardKeys = BuildKeyboardKeys();

        _hotkeyConfig = new HotkeyConfig {
            Mode = HotkeyMode.Toggle,
            Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt,
            VirtualKeyCode = 0x75
        };

        InitializeControls();
        LoadSettings();
        UpdateLimitEditors();
        UpdateActionButtons();
        UpdateStats(0, TimeSpan.Zero);
        SetHeaderStatus(LocalizationService.GetString("Ui.Status.Idle", "Idle"));

        PreviewKeyDown += MainWindow_PreviewKeyDown;
        Closing += MainWindow_Closing;
        Loaded += MainWindow_Loaded;
    }

    protected override void OnSourceInitialized(EventArgs e) {
        base.OnSourceInitialized(e);

        var helper = new WindowInteropHelper(this);
        var handle = helper.Handle;

        _hotkeyService.AttachWindow(handle);
        _windowSource = HwndSource.FromHwnd(handle);
        _windowSource?.AddHook(WindowHook);

        _isWindowReady = true;
        RegisterHotkey();
    }

    private void InitializeControls() {
        ActionTypeComboBox.ItemsSource = _actionModes;
        ActionTypeComboBox.DisplayMemberPath = nameof(OptionItem<ActionType>.Label);
        ActionTypeComboBox.SelectedIndex = 0;

        HotkeyModeComboBox.ItemsSource = _hotkeyModes;
        HotkeyModeComboBox.DisplayMemberPath = nameof(OptionItem<HotkeyMode>.Label);
        HotkeyModeComboBox.SelectedIndex = 0;

        KeyboardKeyComboBox.ItemsSource = _keyboardKeys;
        KeyboardKeyComboBox.DisplayMemberPath = nameof(KeyOption.Label);
        var defaultKey =
            _keyboardKeys.FirstOrDefault(static item => item.VirtualKeyCode == 0x41 && !item.IsGroupHeader);
        KeyboardKeyComboBox.SelectedItem = defaultKey ?? _keyboardKeys.First(static item => !item.IsGroupHeader);

        ClicksPerSecondTextBox.Text = FormatDouble(10.0);
        IntervalMsTextBox.Text = FormatDouble(100.0);
        VariationTextBox.Text = FormatDouble(0.0);
        DutyMinTextBox.Text = "35";
        DutyMaxTextBox.Text = "65";

        EnableClickLimitCheckBox.IsChecked = false;
        ClickLimitTextBox.Text = "100";
        EnableTimeLimitCheckBox.IsChecked = false;
        TimeLimitSecondsTextBox.Text = "30";

        HotkeyDisplayBadge.Value = FormatHotkey(_hotkeyConfig);

        ActionTypeComboBox.SelectionChanged += ActionTypeComboBox_SelectionChanged;
        HotkeyModeComboBox.SelectionChanged += HotkeyModeComboBox_SelectionChanged;
        EnableClickLimitCheckBox.Checked += LimitCheckBox_Changed;
        EnableClickLimitCheckBox.Unchecked += LimitCheckBox_Changed;
        EnableTimeLimitCheckBox.Checked += LimitCheckBox_Changed;
        EnableTimeLimitCheckBox.Unchecked += LimitCheckBox_Changed;

        ClicksPerSecondTextBox.TextChanged += ClicksPerSecondTextBox_TextChanged;
        IntervalMsTextBox.TextChanged += IntervalMsTextBox_TextChanged;

        ClicksPerSecondTextBox.LostFocus += NumericTextBox_LostFocus;
        IntervalMsTextBox.LostFocus += NumericTextBox_LostFocus;
        VariationTextBox.LostFocus += NumericTextBox_LostFocus;
        DutyMinTextBox.LostFocus += NumericTextBox_LostFocus;
        DutyMaxTextBox.LostFocus += NumericTextBox_LostFocus;
        ClickLimitTextBox.LostFocus += NumericTextBox_LostFocus;
        TimeLimitSecondsTextBox.LostFocus += NumericTextBox_LostFocus;

        RecordHotkeyButton.Click += RecordHotkeyButton_Click;
        StartButton.Click += StartButton_Click;
        StopButton.Click += StopButton_Click;

        UpdateActionTypeEditorVisibility();
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e) {
        _ = sender;
        _ = e;
        await StartRunAsync().ConfigureAwait(false);
    }

    private void StopButton_Click(object sender, RoutedEventArgs e) {
        _ = sender;
        _ = e;
        StopRun();
    }

    private void RecordHotkeyButton_Click(object sender, RoutedEventArgs e) {
        _ = sender;
        _ = e;

        _isCapturingHotkey = true;
        SetHeaderStatus(LocalizationService.GetString("Ui.Status.HotkeyCapturePrompt",
            "Recording hotkey: press combination now (ESC to cancel)."));
        _ = Focus();
    }

    private void
        ActionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        _ = sender;
        _ = e;
        UpdateActionTypeEditorVisibility();
    }

    private void
        HotkeyModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        _ = sender;
        _ = e;
        if (HotkeyModeComboBox.SelectedItem is OptionItem<HotkeyMode> selected) {
            _hotkeyConfig.Mode = selected.Value;
            HotkeyDisplayBadge.Value = FormatHotkey(_hotkeyConfig);
            RegisterHotkey();
        }
    }

    private void LimitCheckBox_Changed(object sender, RoutedEventArgs e) {
        _ = sender;
        _ = e;
        UpdateLimitEditors();
    }

    private void ClicksPerSecondTextBox_TextChanged(object sender, TextChangedEventArgs e) {
        _ = sender;
        _ = e;
        if (_isSynchronizingRateFields) return;

        if (!TryParseDouble(ClicksPerSecondTextBox.Text, out var cps) || cps <= 0.0) return;

        cps = Clamp(cps, MinimumClicksPerSecond, MaximumClicksPerSecond);
        var interval = Clamp(1000.0 / cps, MinimumIntervalMilliseconds, MaximumIntervalMilliseconds);

        _isSynchronizingRateFields = true;
        IntervalMsTextBox.Text = FormatDouble(interval);
        _isSynchronizingRateFields = false;
    }

    private void IntervalMsTextBox_TextChanged(object sender, TextChangedEventArgs e) {
        _ = sender;
        _ = e;
        if (_isSynchronizingRateFields) return;

        if (!TryParseDouble(IntervalMsTextBox.Text, out var intervalMs) || intervalMs <= 0.0) return;

        intervalMs = Clamp(intervalMs, MinimumIntervalMilliseconds, MaximumIntervalMilliseconds);
        var cps = Clamp(1000.0 / intervalMs, MinimumClicksPerSecond, MaximumClicksPerSecond);

        _isSynchronizingRateFields = true;
        ClicksPerSecondTextBox.Text = FormatDouble(cps);
        _isSynchronizingRateFields = false;
    }

    private void NumericTextBox_LostFocus(object sender, RoutedEventArgs e) {
        _ = sender;
        _ = e;
        NormalizeNumericFields();
    }

    private async Task StartRunAsync() {
        if (_isRunning) return;

        var profile = BuildProfile();
        var errors = Validation.Validate(profile);
        if (errors.Count > 0) {
            SetHeaderStatus(errors[0]);
            return;
        }

        _isRunning = true;
        UpdateActionButtons();
        SetHeaderStatus(LocalizationService.GetString("Ui.Status.Running", "Running"));
        UpdateStats(0, TimeSpan.Zero);

        _runCancellation = new CancellationTokenSource();

        RunSummary summary;
        try {
            summary = await _engine.RunAsync(
                profile,
                _runCancellation.Token,
                progress => {
                    _ = Dispatcher.BeginInvoke(() => { UpdateStats(progress.ClickCount, progress.Elapsed); });
                }).ConfigureAwait(false);
        } catch (ArgumentException ex) {
            _ = Dispatcher.BeginInvoke(() => {
                _isRunning = false;
                UpdateActionButtons();
                SetHeaderStatus(ex.Message);
            });
            return;
        } catch (Exception ex) {
            _ = Dispatcher.BeginInvoke(() => {
                _isRunning = false;
                UpdateActionButtons();
                SetHeaderStatus(LocalizationService.Format("Ui.Status.ErrorPrefix", ex.Message));
            });
            return;
        }

        _ = Dispatcher.BeginInvoke(() => {
            _isRunning = false;
            UpdateActionButtons();
            SetHeaderStatus(summary.Reason switch {
                StopReason.ClickLimitReached => LocalizationService.GetString("Ui.Status.StoppedClickLimit",
                    "Stopped (click limit reached)"),
                StopReason.TimeLimitReached => LocalizationService.GetString("Ui.Status.StoppedTimeLimit",
                    "Stopped (time limit reached)"),
                _ => LocalizationService.GetString("Ui.Status.Stopped", "Stopped")
            });
            UpdateStats(summary.ClickCount, summary.Elapsed);
        });
    }

    private void StopRun() {
        _runCancellation?.Cancel();
    }

    private ClickProfile BuildProfile() {
        var cps = ParseDoubleOrDefault(ClicksPerSecondTextBox.Text, 10.0);
        cps = Clamp(cps, MinimumClicksPerSecond, MaximumClicksPerSecond);

        var variation = ParseDoubleOrDefault(VariationTextBox.Text, 0.0);
        variation = Clamp(variation, 0.0, 95.0);

        var dutyMin = ParseIntOrDefault(DutyMinTextBox.Text, 35);
        var dutyMax = ParseIntOrDefault(DutyMaxTextBox.Text, 65);

        int? clickLimit = null;
        if (EnableClickLimitCheckBox.IsChecked == true) {
            var limit = ParseIntOrDefault(ClickLimitTextBox.Text, 100);
            clickLimit = Math.Max(1, limit);
        }

        TimeSpan? timeLimit = null;
        if (EnableTimeLimitCheckBox.IsChecked == true) {
            var seconds = ParseIntOrDefault(TimeLimitSecondsTextBox.Text, 30);
            timeLimit = TimeSpan.FromSeconds(Math.Max(1, seconds));
        }

        var actionType = ActionType.MouseLeft;
        if (ActionTypeComboBox.SelectedItem is OptionItem<ActionType> actionItem) actionType = actionItem.Value;

        var virtualKeyCode = 0;
        if (actionType == ActionType.KeyboardKey && KeyboardKeyComboBox.SelectedItem is KeyOption keyOption &&
            !keyOption.IsGroupHeader) virtualKeyCode = keyOption.VirtualKeyCode;

        return new ClickProfile {
            ClicksPerSecond = cps,
            SpeedVariationPercent = variation,
            DutyCycleMinPercent = dutyMin,
            DutyCycleMaxPercent = dutyMax,
            ClickLimit = clickLimit,
            TimeLimit = timeLimit,
            ActionType = actionType,
            VirtualKeyCode = virtualKeyCode
        };
    }

    private void HotkeyService_Pressed(object? sender, EventArgs e) {
        _ = sender;
        _ = e;

        var now = DateTime.UtcNow;
        if ((now - _lastHotkeyPressUtc).TotalMilliseconds < 220) return;

        _lastHotkeyPressUtc = now;

        if (_hotkeyConfig.Mode == HotkeyMode.Toggle) {
            if (_isRunning) {
                StopRun();
                return;
            }

            _ = StartRunAsync();
            return;
        }

        if (!_isRunning) _ = StartRunAsync();

        _holdModeTimer.Start();
    }

    private void HoldModeTimer_Tick(object? sender, EventArgs e) {
        _ = sender;
        _ = e;

        if (_hotkeyConfig.Mode != HotkeyMode.Hold) {
            _holdModeTimer.Stop();
            return;
        }

        if (!_isRunning) {
            _holdModeTimer.Stop();
            return;
        }

        if (!_hotkeyService.IsCurrentHotkeyPressed()) {
            StopRun();
            _holdModeTimer.Stop();
        }
    }

    private void RegisterHotkey() {
        HotkeyDisplayBadge.Value = FormatHotkey(_hotkeyConfig);

        if (!_isWindowReady) return;

        var registered = _hotkeyService.Register(_hotkeyConfig);
        if (!registered)
            SetHeaderStatus(LocalizationService.GetString("Ui.Status.HotkeyRegisterFailed",
                "Hotkey registration failed. Choose another combination."));
    }

    private IntPtr WindowHook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled) {
        _ = hwnd;
        _ = lParam;

        _hotkeyService.ProcessWindowMessage(message, wParam);
        if (message == GlobalHotkeyService.WmHotkey) handled = true;

        return IntPtr.Zero;
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e) {
        _ = sender;

        if (!_isCapturingHotkey) return;

        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape) {
            _isCapturingHotkey = false;
            SetHeaderStatus(LocalizationService.GetString("Ui.Status.HotkeyCaptureCancelled",
                "Hotkey capture cancelled."));
            return;
        }

        if (IsModifierKey(key)) {
            SetHeaderStatus(LocalizationService.GetString("Ui.Status.HotkeyNeedNonModifier",
                "Please press at least one non-modifier key."));
            return;
        }

        var modifiers = ReadModifiers();
        if (modifiers == HotkeyModifiers.None) {
            SetHeaderStatus(LocalizationService.GetString("Ui.Status.HotkeyNeedModifier",
                "Hotkey must include at least one modifier."));
            return;
        }

        var vk = KeyInterop.VirtualKeyFromKey(key);
        _hotkeyConfig = new HotkeyConfig {
            Modifiers = modifiers,
            VirtualKeyCode = vk,
            Mode = _hotkeyConfig.Mode
        };

        _isCapturingHotkey = false;
        RegisterHotkey();
        SetHeaderStatus(LocalizationService.GetString("Ui.Status.HotkeyUpdated", "Hotkey updated."));
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e) {
        _ = sender;
        _ = e;

        StopRun();
        _holdModeTimer.Stop();
        _hotkeyService.Unregister();
        _windowSource?.RemoveHook(WindowHook);
        SaveSettings();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
        _ = sender;
        _ = e;
        ApplyAutoHeight();
    }

    private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        _ = sender;

        if (!ReferenceEquals(e.OriginalSource, MainTabControl)) return;

        ApplyAutoHeight();
    }

    private void UpdateActionTypeEditorVisibility() {
        var isKeyboard = false;
        if (ActionTypeComboBox.SelectedItem is OptionItem<ActionType> selected)
            isKeyboard = selected.Value == ActionType.KeyboardKey;

        KeyboardKeyRow.Visibility = isKeyboard ? Visibility.Visible : Visibility.Collapsed;
        ApplyAutoHeight();
    }

    private void UpdateLimitEditors() {
        ClickLimitTextBox.IsEnabled = EnableClickLimitCheckBox.IsChecked == true;
        TimeLimitSecondsTextBox.IsEnabled = EnableTimeLimitCheckBox.IsChecked == true;
    }

    private void UpdateActionButtons() {
        StartButton.IsEnabled = !_isRunning;
        StopButton.IsEnabled = _isRunning;
    }

    private void UpdateStats(int clickCount, TimeSpan elapsed) {
        ClicksTextBlock.Text =
            LocalizationService.Format("Ui.Header.Clicks.Format", clickCount.ToString(CultureInfo.CurrentCulture));
        ElapsedTextBlock.Text = LocalizationService.Format("Ui.Header.Elapsed.Format",
            elapsed.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture));
    }

    private void SetHeaderStatus(string text) {
        var safeText = string.IsNullOrWhiteSpace(text)
            ? LocalizationService.GetString("Ui.Status.Idle", "Idle")
            : text;
        StatusTextBlock.Text = LocalizationService.Format("Ui.Header.Status.Format", safeText);
    }

    private void ApplyAutoHeight() {
        if (!IsLoaded || _isApplyingAutoHeight) return;

        _ = Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
            if (_isApplyingAutoHeight) return;

            _isApplyingAutoHeight = true;
            try {
                MaxHeight = SystemParameters.WorkArea.Height;
                SizeToContent = SizeToContent.Manual;
                UpdateLayout();
                SizeToContent = SizeToContent.Height;
            } finally {
                _isApplyingAutoHeight = false;
            }
        }));
    }

    private void NormalizeNumericFields() {
        var cps = ParseDoubleOrDefault(ClicksPerSecondTextBox.Text, 10.0);
        cps = Clamp(cps, MinimumClicksPerSecond, MaximumClicksPerSecond);
        var interval = Clamp(1000.0 / cps, MinimumIntervalMilliseconds, MaximumIntervalMilliseconds);

        var variation = ParseDoubleOrDefault(VariationTextBox.Text, 0.0);
        variation = Clamp(variation, 0.0, 95.0);

        var dutyMin = ParseIntOrDefault(DutyMinTextBox.Text, 35);
        dutyMin = Math.Clamp(dutyMin, 1, 99);

        var dutyMax = ParseIntOrDefault(DutyMaxTextBox.Text, 65);
        dutyMax = Math.Clamp(dutyMax, 1, 99);

        var clickLimit = ParseIntOrDefault(ClickLimitTextBox.Text, 100);
        clickLimit = Math.Clamp(clickLimit, 1, 1_000_000);

        var timeLimitSeconds = ParseIntOrDefault(TimeLimitSecondsTextBox.Text, 30);
        timeLimitSeconds = Math.Clamp(timeLimitSeconds, 1, 86_400);

        _isSynchronizingRateFields = true;
        ClicksPerSecondTextBox.Text = FormatDouble(cps);
        IntervalMsTextBox.Text = FormatDouble(interval);
        VariationTextBox.Text = FormatDouble(variation);
        DutyMinTextBox.Text = dutyMin.ToString(CultureInfo.CurrentCulture);
        DutyMaxTextBox.Text = dutyMax.ToString(CultureInfo.CurrentCulture);
        ClickLimitTextBox.Text = clickLimit.ToString(CultureInfo.CurrentCulture);
        TimeLimitSecondsTextBox.Text = timeLimitSeconds.ToString(CultureInfo.CurrentCulture);
        _isSynchronizingRateFields = false;
    }

    private void SaveSettings() {
        var profile = BuildProfile();

        var clickLimitValue = ParseIntOrDefault(ClickLimitTextBox.Text, 100);
        var timeLimitSeconds = ParseIntOrDefault(TimeLimitSecondsTextBox.Text, 30);

        var settings = new AppSettings {
            Profile = profile,
            IsClickLimitEnabled = EnableClickLimitCheckBox.IsChecked == true,
            ClickLimitValue = Math.Clamp(clickLimitValue, 1, 1_000_000),
            IsTimeLimitEnabled = EnableTimeLimitCheckBox.IsChecked == true,
            TimeLimitSeconds = Math.Clamp(timeLimitSeconds, 1, 86_400),
            Hotkey = new HotkeyConfig {
                Mode = _hotkeyConfig.Mode,
                Modifiers = _hotkeyConfig.Modifiers,
                VirtualKeyCode = _hotkeyConfig.VirtualKeyCode
            }
        };

        SettingsStore.Save(settings);
    }

    private void LoadSettings() {
        var settings = SettingsStore.Load();
        var profile = settings.Profile;

        ClicksPerSecondTextBox.Text =
            FormatDouble(Clamp(profile.ClicksPerSecond, MinimumClicksPerSecond, MaximumClicksPerSecond));
        VariationTextBox.Text = FormatDouble(Clamp(profile.SpeedVariationPercent, 0.0, 95.0));
        DutyMinTextBox.Text = Math.Clamp(profile.DutyCycleMinPercent, 1, 99).ToString(CultureInfo.CurrentCulture);
        DutyMaxTextBox.Text = Math.Clamp(profile.DutyCycleMaxPercent, 1, 99).ToString(CultureInfo.CurrentCulture);
        EnableClickLimitCheckBox.IsChecked = settings.IsClickLimitEnabled;
        ClickLimitTextBox.Text =
            Math.Clamp(settings.ClickLimitValue, 1, 1_000_000).ToString(CultureInfo.CurrentCulture);
        EnableTimeLimitCheckBox.IsChecked = settings.IsTimeLimitEnabled;
        TimeLimitSecondsTextBox.Text =
            Math.Clamp(settings.TimeLimitSeconds, 1, 86_400).ToString(CultureInfo.CurrentCulture);

        _isSynchronizingRateFields = true;
        if (TryParseDouble(ClicksPerSecondTextBox.Text, out var cps)) {
            var interval = Clamp(1000.0 / cps, MinimumIntervalMilliseconds, MaximumIntervalMilliseconds);
            IntervalMsTextBox.Text = FormatDouble(interval);
        }

        _isSynchronizingRateFields = false;

        SelectActionMode(profile.ActionType);
        SelectKeyboardKey(profile.VirtualKeyCode <= 0 ? 0x41 : profile.VirtualKeyCode);

        _hotkeyConfig = settings.Hotkey ?? new HotkeyConfig();
        SelectHotkeyMode(_hotkeyConfig.Mode);
        HotkeyDisplayBadge.Value = FormatHotkey(_hotkeyConfig);
    }

    private void SelectActionMode(ActionType actionType) {
        var item = _actionModes.FirstOrDefault(x => x.Value == actionType);
        if (item is not null) ActionTypeComboBox.SelectedItem = item;
    }

    private void SelectHotkeyMode(HotkeyMode mode) {
        var item = _hotkeyModes.FirstOrDefault(x => x.Value == mode);
        if (item is not null) HotkeyModeComboBox.SelectedItem = item;
    }

    private void SelectKeyboardKey(int virtualKeyCode) {
        var option = _keyboardKeys.FirstOrDefault(x => x.VirtualKeyCode == virtualKeyCode && !x.IsGroupHeader);
        if (option is null)
            option = _keyboardKeys.FirstOrDefault(static x => x.VirtualKeyCode == 0x41 && !x.IsGroupHeader);

        if (option is not null) KeyboardKeyComboBox.SelectedItem = option;
    }

    private static List<KeyOption> BuildKeyboardKeys() {
        var options = new List<KeyOption>(220);
        var usedVirtualKeys = new HashSet<int>();

        AddGroup(
            options,
            LocalizationService.GetString("Ui.Keyboard.Group.Alphabet", "Alphabet"),
            Enumerable.Range((int)Key.A, (int)Key.Z - (int)Key.A + 1).Select(static value => (Key)value),
            usedVirtualKeys);

        AddGroup(
            options,
            LocalizationService.GetString("Ui.Keyboard.Group.Numbers", "Numbers"),
            Enumerable.Range((int)Key.D0, (int)Key.D9 - (int)Key.D0 + 1).Select(static value => (Key)value),
            usedVirtualKeys);

        AddGroup(
            options,
            LocalizationService.GetString("Ui.Keyboard.Group.FunctionKeys", "Function Keys"),
            Enumerable.Range((int)Key.F1, (int)Key.F12 - (int)Key.F1 + 1).Select(static value => (Key)value),
            usedVirtualKeys);

        AddGroup(
            options,
            LocalizationService.GetString("Ui.Keyboard.Group.Numpad", "Numpad"),
            new[] {
                Key.NumPad0, Key.NumPad1, Key.NumPad2, Key.NumPad3, Key.NumPad4,
                Key.NumPad5, Key.NumPad6, Key.NumPad7, Key.NumPad8, Key.NumPad9,
                Key.Decimal, Key.Add, Key.Subtract, Key.Multiply, Key.Divide
            },
            usedVirtualKeys);

        var otherKeys = new List<KeyOption>(120);
        foreach (var key in Enum.GetValues<Key>()) {
            if (!TryCreateKeyOption(key, usedVirtualKeys, out var option)) continue;

            otherKeys.Add(option);
        }

        otherKeys.Sort(static (left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Label, right.Label));
        if (otherKeys.Count > 0) {
            options.Add(
                KeyOption.CreateGroupHeader(LocalizationService.GetString("Ui.Keyboard.Group.Other", "Other Keys")));
            options.AddRange(otherKeys);
        }

        return options;
    }

    private static void AddGroup(List<KeyOption> target, string headerLabel, IEnumerable<Key> keys,
        HashSet<int> usedVirtualKeys) {
        var groupItems = new List<KeyOption>();
        foreach (var key in keys) {
            if (!TryCreateKeyOption(key, usedVirtualKeys, out var option)) continue;

            groupItems.Add(option);
        }

        if (groupItems.Count == 0) return;

        target.Add(KeyOption.CreateGroupHeader(headerLabel));
        target.AddRange(groupItems);
    }

    private static bool TryCreateKeyOption(Key key, HashSet<int> usedVirtualKeys, out KeyOption option) {
        option = null!;

        if (key == Key.None || IsModifierKey(key)) return false;

        int virtualKeyCode;
        try {
            virtualKeyCode = KeyInterop.VirtualKeyFromKey(key);
        } catch {
            return false;
        }

        if (virtualKeyCode <= 0 || virtualKeyCode > 255) return false;

        if (!usedVirtualKeys.Add(virtualKeyCode)) return false;

        var keyDisplayName = key switch {
            >= Key.A and <= Key.Z => key.ToString(),
            >= Key.D0 and <= Key.D9 => ((char)('0' + ((int)key - (int)Key.D0))).ToString(CultureInfo.InvariantCulture),
            >= Key.NumPad0 and <= Key.NumPad9 => $"Num {(int)key - (int)Key.NumPad0}",
            _ => key.ToString()
        };

        option = new KeyOption(virtualKeyCode, $"{keyDisplayName} (VK 0x{virtualKeyCode:X2})");
        return true;
    }

    private static string FormatHotkey(HotkeyConfig config) {
        var builder = new StringBuilder(32);

        if (config.Modifiers.HasFlag(HotkeyModifiers.Control)) builder.Append("Ctrl + ");

        if (config.Modifiers.HasFlag(HotkeyModifiers.Shift)) builder.Append("Shift + ");

        if (config.Modifiers.HasFlag(HotkeyModifiers.Alt)) builder.Append("Alt + ");

        if (config.Modifiers.HasFlag(HotkeyModifiers.Win)) builder.Append("Win + ");

        builder.Append(GetKeyDisplayName(config.VirtualKeyCode));
        return builder.ToString();
    }

    private static string GetKeyDisplayName(int virtualKeyCode) {
        var key = KeyInterop.KeyFromVirtualKey(virtualKeyCode);
        if (key == Key.None) return $"VK 0x{virtualKeyCode:X2}";

        return key.ToString();
    }

    private static HotkeyModifiers ReadModifiers() {
        var modifiers = HotkeyModifiers.None;

        var keyboardModifiers = Keyboard.Modifiers;
        if (keyboardModifiers.HasFlag(ModifierKeys.Control)) modifiers |= HotkeyModifiers.Control;

        if (keyboardModifiers.HasFlag(ModifierKeys.Shift)) modifiers |= HotkeyModifiers.Shift;

        if (keyboardModifiers.HasFlag(ModifierKeys.Alt)) modifiers |= HotkeyModifiers.Alt;

        if (IsWinDown()) modifiers |= HotkeyModifiers.Win;

        return modifiers;
    }

    private static bool IsWinDown() {
        return Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin);
    }

    private static bool IsModifierKey(Key key) {
        return key is Key.LeftCtrl
            or Key.RightCtrl
            or Key.LeftShift
            or Key.RightShift
            or Key.LeftAlt
            or Key.RightAlt
            or Key.LWin
            or Key.RWin
            or Key.System;
    }

    private static bool TryParseDouble(string? input, out double value) {
        var text = (input ?? string.Empty).Trim();

        var parsed = double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        if (parsed) return true;

        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static double ParseDoubleOrDefault(string? input, double fallback) {
        return TryParseDouble(input, out var value) ? value : fallback;
    }

    private static int ParseIntOrDefault(string? input, int fallback) {
        var text = (input ?? string.Empty).Trim();
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var value)) return value;

        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)) return value;

        return fallback;
    }

    private static double Clamp(double value, double min, double max) {
        return Math.Min(max, Math.Max(min, value));
    }

    private static string FormatDouble(double value) {
        return Math.Round(value, 2).ToString("0.##", CultureInfo.CurrentCulture);
    }
}