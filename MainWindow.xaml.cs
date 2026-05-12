using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace mkAutoClicker;

public partial class MainWindow : Window {
    private const double MinimumClicksPerSecond = 0.1;
    private const double MaximumClicksPerSecond = 1000.0;
    private const double MinimumIntervalMilliseconds = 1.0;
    private const double MaximumIntervalMilliseconds = 10000.0;


    private readonly AutoClickerEngine engine;
    private readonly GlobalHotkeyService hotkeyService;
    private readonly DispatcherTimer holdModeTimer;

    private readonly List<OptionItem<ActionType>> actionModes;
    private readonly List<OptionItem<HotkeyMode>> hotkeyModes;
    private readonly List<KeyOption> keyboardKeys;

    private HwndSource? windowSource;
    private CancellationTokenSource? runCancellation;

    private HotkeyConfig hotkeyConfig;

    private bool isRunning;
    private bool isCapturingHotkey;
    private bool isSynchronizingRateFields;
    private bool isWindowReady;
    private bool isApplyingAutoHeight;
    private DateTime lastHotkeyPressUtc = DateTime.MinValue;

    public MainWindow() {
        this.InitializeComponent();
        this.MaxHeight = SystemParameters.WorkArea.Height;

        this.engine = new AutoClickerEngine();
        this.hotkeyService = new GlobalHotkeyService();
        this.hotkeyService.Pressed += this.HotkeyService_Pressed;

        this.holdModeTimer = new DispatcherTimer {
            Interval = TimeSpan.FromMilliseconds(35)
        };
        this.holdModeTimer.Tick += this.HoldModeTimer_Tick;

        this.actionModes = new List<OptionItem<ActionType>> {
            new OptionItem<ActionType>("Mouse Left", ActionType.MouseLeft),
            new OptionItem<ActionType>("Mouse Middle", ActionType.MouseMiddle),
            new OptionItem<ActionType>("Mouse Right", ActionType.MouseRight),
            new OptionItem<ActionType>("Keyboard Key", ActionType.KeyboardKey)
        };
        this.hotkeyModes = new List<OptionItem<HotkeyMode>> {
            new OptionItem<HotkeyMode>("Toggle", HotkeyMode.Toggle),
            new OptionItem<HotkeyMode>("Hold", HotkeyMode.Hold)
        };
        this.keyboardKeys = BuildKeyboardKeys();

        this.hotkeyConfig = new HotkeyConfig {
            Mode = HotkeyMode.Toggle,
            Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt,
            VirtualKeyCode = 0x75
        };

        this.InitializeControls();
        this.LoadSettings();
        this.UpdateLimitEditors();
        this.UpdateActionButtons();
        this.UpdateStats(0, TimeSpan.Zero);
        this.SetHeaderStatus("Idle");

        this.PreviewKeyDown += this.MainWindow_PreviewKeyDown;
        this.Closing += this.MainWindow_Closing;
        this.Loaded += this.MainWindow_Loaded;
    }

    protected override void OnSourceInitialized(EventArgs e) {
        base.OnSourceInitialized(e);

        WindowInteropHelper helper = new WindowInteropHelper(this);
        IntPtr handle = helper.Handle;

        this.hotkeyService.AttachWindow(handle);
        this.windowSource = HwndSource.FromHwnd(handle);
        this.windowSource?.AddHook(this.WindowHook);

        this.isWindowReady = true;
        this.RegisterHotkey();
    }

    private void InitializeControls() {
        this.ActionTypeComboBox.ItemsSource = this.actionModes;
        this.ActionTypeComboBox.DisplayMemberPath = nameof(OptionItem<ActionType>.Label);
        this.ActionTypeComboBox.SelectedIndex = 0;

        this.HotkeyModeComboBox.ItemsSource = this.hotkeyModes;
        this.HotkeyModeComboBox.DisplayMemberPath = nameof(OptionItem<HotkeyMode>.Label);
        this.HotkeyModeComboBox.SelectedIndex = 0;

        this.KeyboardKeyComboBox.ItemsSource = this.keyboardKeys;
        this.KeyboardKeyComboBox.DisplayMemberPath = nameof(KeyOption.Label);
        KeyOption? defaultKey = this.keyboardKeys.FirstOrDefault(static item => item.VirtualKeyCode == 0x41);
        this.KeyboardKeyComboBox.SelectedItem = defaultKey ?? this.keyboardKeys.First();

        this.ClicksPerSecondTextBox.Text = FormatDouble(10.0);
        this.IntervalMsTextBox.Text = FormatDouble(100.0);
        this.VariationTextBox.Text = FormatDouble(0.0);
        this.DutyMinTextBox.Text = "35";
        this.DutyMaxTextBox.Text = "65";

        this.EnableClickLimitCheckBox.IsChecked = false;
        this.ClickLimitTextBox.Text = "100";
        this.EnableTimeLimitCheckBox.IsChecked = false;
        this.TimeLimitSecondsTextBox.Text = "30";

        this.HotkeyDisplayBadge.Value = FormatHotkey(this.hotkeyConfig);

        this.ActionTypeComboBox.SelectionChanged += this.ActionTypeComboBox_SelectionChanged;
        this.HotkeyModeComboBox.SelectionChanged += this.HotkeyModeComboBox_SelectionChanged;
        this.EnableClickLimitCheckBox.Checked += this.LimitCheckBox_Changed;
        this.EnableClickLimitCheckBox.Unchecked += this.LimitCheckBox_Changed;
        this.EnableTimeLimitCheckBox.Checked += this.LimitCheckBox_Changed;
        this.EnableTimeLimitCheckBox.Unchecked += this.LimitCheckBox_Changed;

        this.ClicksPerSecondTextBox.TextChanged += this.ClicksPerSecondTextBox_TextChanged;
        this.IntervalMsTextBox.TextChanged += this.IntervalMsTextBox_TextChanged;

        this.ClicksPerSecondTextBox.LostFocus += this.NumericTextBox_LostFocus;
        this.IntervalMsTextBox.LostFocus += this.NumericTextBox_LostFocus;
        this.VariationTextBox.LostFocus += this.NumericTextBox_LostFocus;
        this.DutyMinTextBox.LostFocus += this.NumericTextBox_LostFocus;
        this.DutyMaxTextBox.LostFocus += this.NumericTextBox_LostFocus;
        this.ClickLimitTextBox.LostFocus += this.NumericTextBox_LostFocus;
        this.TimeLimitSecondsTextBox.LostFocus += this.NumericTextBox_LostFocus;

        this.RecordHotkeyButton.Click += this.RecordHotkeyButton_Click;
        this.StartButton.Click += this.StartButton_Click;
        this.StopButton.Click += this.StopButton_Click;

        this.UpdateActionTypeEditorVisibility();
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e) {
        _ = sender;
        _ = e;
        await this.StartRunAsync().ConfigureAwait(false);
    }

    private void StopButton_Click(object sender, RoutedEventArgs e) {
        _ = sender;
        _ = e;
        this.StopRun();
    }

    private void RecordHotkeyButton_Click(object sender, RoutedEventArgs e) {
        _ = sender;
        _ = e;

        this.isCapturingHotkey = true;
        this.SetHeaderStatus("Hotkey aufnehmen: Kombination jetzt druecken (ESC = Abbruch).");
        _ = this.Focus();
    }

    private void
        ActionTypeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
        _ = sender;
        _ = e;
        this.UpdateActionTypeEditorVisibility();
    }

    private void
        HotkeyModeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
        _ = sender;
        _ = e;
        if (this.HotkeyModeComboBox.SelectedItem is OptionItem<HotkeyMode> selected) {
            this.hotkeyConfig.Mode = selected.Value;
            this.HotkeyDisplayBadge.Value = FormatHotkey(this.hotkeyConfig);
            this.RegisterHotkey();
        }
    }

    private void LimitCheckBox_Changed(object sender, RoutedEventArgs e) {
        _ = sender;
        _ = e;
        this.UpdateLimitEditors();
    }

    private void ClicksPerSecondTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
        _ = sender;
        _ = e;
        if (this.isSynchronizingRateFields) {
            return;
        }

        if (!TryParseDouble(this.ClicksPerSecondTextBox.Text, out double cps) || cps <= 0.0) {
            return;
        }

        cps = Clamp(cps, MinimumClicksPerSecond, MaximumClicksPerSecond);
        double interval = Clamp(1000.0 / cps, MinimumIntervalMilliseconds, MaximumIntervalMilliseconds);

        this.isSynchronizingRateFields = true;
        this.IntervalMsTextBox.Text = FormatDouble(interval);
        this.isSynchronizingRateFields = false;
    }

    private void IntervalMsTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
        _ = sender;
        _ = e;
        if (this.isSynchronizingRateFields) {
            return;
        }

        if (!TryParseDouble(this.IntervalMsTextBox.Text, out double intervalMs) || intervalMs <= 0.0) {
            return;
        }

        intervalMs = Clamp(intervalMs, MinimumIntervalMilliseconds, MaximumIntervalMilliseconds);
        double cps = Clamp(1000.0 / intervalMs, MinimumClicksPerSecond, MaximumClicksPerSecond);

        this.isSynchronizingRateFields = true;
        this.ClicksPerSecondTextBox.Text = FormatDouble(cps);
        this.isSynchronizingRateFields = false;
    }

    private void NumericTextBox_LostFocus(object sender, RoutedEventArgs e) {
        _ = sender;
        _ = e;
        this.NormalizeNumericFields();
    }

    private async Task StartRunAsync() {
        if (this.isRunning) {
            return;
        }

        ClickProfile profile = this.BuildProfile();
        IReadOnlyList<string> errors = Validation.Validate(profile);
        if (errors.Count > 0) {
            this.SetHeaderStatus(errors[0]);
            return;
        }

        this.isRunning = true;
        this.UpdateActionButtons();
        this.SetHeaderStatus("Running");
        this.UpdateStats(0, TimeSpan.Zero);

        this.runCancellation = new CancellationTokenSource();

        RunSummary summary;
        try {
            summary = await this.engine.RunAsync(
                profile,
                this.runCancellation.Token,
                progress => {
                    _ = this.Dispatcher.BeginInvoke(() => { this.UpdateStats(progress.ClickCount, progress.Elapsed); });
                }).ConfigureAwait(false);
        }
        catch (ArgumentException ex) {
            _ = this.Dispatcher.BeginInvoke(() => {
                this.isRunning = false;
                this.UpdateActionButtons();
                this.SetHeaderStatus(ex.Message);
            });
            return;
        }
        catch (Exception ex) {
            _ = this.Dispatcher.BeginInvoke(() => {
                this.isRunning = false;
                this.UpdateActionButtons();
                this.SetHeaderStatus($"Fehler: {ex.Message}");
            });
            return;
        }

        _ = this.Dispatcher.BeginInvoke(() => {
            this.isRunning = false;
            this.UpdateActionButtons();
            this.SetHeaderStatus(summary.Reason switch {
                StopReason.ClickLimitReached => "Stopped (Click-Limit erreicht)",
                StopReason.TimeLimitReached => "Stopped (Time-Limit erreicht)",
                _ => "Stopped"
            });
            this.UpdateStats(summary.ClickCount, summary.Elapsed);
        });
    }

    private void StopRun() {
        this.runCancellation?.Cancel();
    }

    private ClickProfile BuildProfile() {
        double cps = ParseDoubleOrDefault(this.ClicksPerSecondTextBox.Text, 10.0);
        cps = Clamp(cps, MinimumClicksPerSecond, MaximumClicksPerSecond);

        double variation = ParseDoubleOrDefault(this.VariationTextBox.Text, 0.0);
        variation = Clamp(variation, 0.0, 95.0);

        int dutyMin = ParseIntOrDefault(this.DutyMinTextBox.Text, 35);
        int dutyMax = ParseIntOrDefault(this.DutyMaxTextBox.Text, 65);

        int? clickLimit = null;
        if (this.EnableClickLimitCheckBox.IsChecked == true) {
            int limit = ParseIntOrDefault(this.ClickLimitTextBox.Text, 100);
            clickLimit = Math.Max(1, limit);
        }

        TimeSpan? timeLimit = null;
        if (this.EnableTimeLimitCheckBox.IsChecked == true) {
            int seconds = ParseIntOrDefault(this.TimeLimitSecondsTextBox.Text, 30);
            timeLimit = TimeSpan.FromSeconds(Math.Max(1, seconds));
        }

        ActionType actionType = ActionType.MouseLeft;
        if (this.ActionTypeComboBox.SelectedItem is OptionItem<ActionType> actionItem) {
            actionType = actionItem.Value;
        }

        int virtualKeyCode = 0;
        if (actionType == ActionType.KeyboardKey && this.KeyboardKeyComboBox.SelectedItem is KeyOption keyOption) {
            virtualKeyCode = keyOption.VirtualKeyCode;
        }

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

        DateTime now = DateTime.UtcNow;
        if ((now - this.lastHotkeyPressUtc).TotalMilliseconds < 220) {
            return;
        }

        this.lastHotkeyPressUtc = now;

        if (this.hotkeyConfig.Mode == HotkeyMode.Toggle) {
            if (this.isRunning) {
                this.StopRun();
                return;
            }

            _ = this.StartRunAsync();
            return;
        }

        if (!this.isRunning) {
            _ = this.StartRunAsync();
        }

        this.holdModeTimer.Start();
    }

    private void HoldModeTimer_Tick(object? sender, EventArgs e) {
        _ = sender;
        _ = e;

        if (this.hotkeyConfig.Mode != HotkeyMode.Hold) {
            this.holdModeTimer.Stop();
            return;
        }

        if (!this.isRunning) {
            this.holdModeTimer.Stop();
            return;
        }

        if (!this.hotkeyService.IsCurrentHotkeyPressed()) {
            this.StopRun();
            this.holdModeTimer.Stop();
        }
    }

    private void RegisterHotkey() {
        this.HotkeyDisplayBadge.Value = FormatHotkey(this.hotkeyConfig);

        if (!this.isWindowReady) {
            return;
        }

        bool registered = this.hotkeyService.Register(this.hotkeyConfig);
        if (!registered) {
            this.SetHeaderStatus("Hotkey konnte nicht registriert werden. Andere Kombination waehlen.");
        }
    }

    private IntPtr WindowHook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled) {
        _ = hwnd;
        _ = lParam;

        this.hotkeyService.ProcessWindowMessage(message, wParam);
        if (message == GlobalHotkeyService.WmHotkey) {
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e) {
        _ = sender;

        if (!this.isCapturingHotkey) {
            return;
        }

        e.Handled = true;

        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape) {
            this.isCapturingHotkey = false;
            this.SetHeaderStatus("Hotkey-Aufnahme abgebrochen.");
            return;
        }

        if (IsModifierKey(key)) {
            this.SetHeaderStatus("Bitte mindestens eine Nicht-Modifier-Taste druecken.");
            return;
        }

        HotkeyModifiers modifiers = ReadModifiers();
        if (modifiers == HotkeyModifiers.None) {
            this.SetHeaderStatus("Hotkey muss mindestens einen Modifier enthalten.");
            return;
        }

        int vk = KeyInterop.VirtualKeyFromKey(key);
        this.hotkeyConfig = new HotkeyConfig {
            Modifiers = modifiers,
            VirtualKeyCode = vk,
            Mode = this.hotkeyConfig.Mode
        };

        this.isCapturingHotkey = false;
        this.RegisterHotkey();
        this.SetHeaderStatus("Hotkey aktualisiert.");
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e) {
        _ = sender;
        _ = e;

        this.StopRun();
        this.holdModeTimer.Stop();
        this.hotkeyService.Unregister();
        this.windowSource?.RemoveHook(this.WindowHook);
        this.SaveSettings();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
        _ = sender;
        _ = e;
        this.ApplyAutoHeight();
    }

    private void MainTabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
        _ = sender;

        if (!ReferenceEquals(e.OriginalSource, this.MainTabControl)) {
            return;
        }

        this.ApplyAutoHeight();
    }

    private void UpdateActionTypeEditorVisibility() {
        bool isKeyboard = false;
        if (this.ActionTypeComboBox.SelectedItem is OptionItem<ActionType> selected) {
            isKeyboard = selected.Value == ActionType.KeyboardKey;
        }

        this.KeyboardKeyRow.Visibility = isKeyboard ? Visibility.Visible : Visibility.Collapsed;
        this.ApplyAutoHeight();
    }

    private void UpdateLimitEditors() {
        this.ClickLimitTextBox.IsEnabled = this.EnableClickLimitCheckBox.IsChecked == true;
        this.TimeLimitSecondsTextBox.IsEnabled = this.EnableTimeLimitCheckBox.IsChecked == true;
    }

    private void UpdateActionButtons() {
        this.StartButton.IsEnabled = !this.isRunning;
        this.StopButton.IsEnabled = this.isRunning;
    }

    private void UpdateStats(int clickCount, TimeSpan elapsed) {
        this.ClicksTextBlock.Text = $"Clicks: {clickCount.ToString(CultureInfo.CurrentCulture)}";
        this.ElapsedTextBlock.Text = $"Elapsed: {elapsed.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)}";
    }

    private void SetHeaderStatus(string text) {
        string safeText = string.IsNullOrWhiteSpace(text) ? "Idle" : text;
        this.StatusTextBlock.Text = $"Status: {safeText}";
    }

    private void ApplyAutoHeight() {
        if (!this.IsLoaded || this.isApplyingAutoHeight) {
            return;
        }

        _ = this.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
            if (this.isApplyingAutoHeight) {
                return;
            }

            this.isApplyingAutoHeight = true;
            try {
                this.MaxHeight = SystemParameters.WorkArea.Height;
                this.SizeToContent = SizeToContent.Manual;
                this.UpdateLayout();
                this.SizeToContent = SizeToContent.Height;
            } finally {
                this.isApplyingAutoHeight = false;
            }
        }));
    }

    private void NormalizeNumericFields() {
        double cps = ParseDoubleOrDefault(this.ClicksPerSecondTextBox.Text, 10.0);
        cps = Clamp(cps, MinimumClicksPerSecond, MaximumClicksPerSecond);
        double interval = Clamp(1000.0 / cps, MinimumIntervalMilliseconds, MaximumIntervalMilliseconds);

        double variation = ParseDoubleOrDefault(this.VariationTextBox.Text, 0.0);
        variation = Clamp(variation, 0.0, 95.0);

        int dutyMin = ParseIntOrDefault(this.DutyMinTextBox.Text, 35);
        dutyMin = Math.Clamp(dutyMin, 1, 99);

        int dutyMax = ParseIntOrDefault(this.DutyMaxTextBox.Text, 65);
        dutyMax = Math.Clamp(dutyMax, 1, 99);

        int clickLimit = ParseIntOrDefault(this.ClickLimitTextBox.Text, 100);
        clickLimit = Math.Clamp(clickLimit, 1, 1_000_000);

        int timeLimitSeconds = ParseIntOrDefault(this.TimeLimitSecondsTextBox.Text, 30);
        timeLimitSeconds = Math.Clamp(timeLimitSeconds, 1, 86_400);

        this.isSynchronizingRateFields = true;
        this.ClicksPerSecondTextBox.Text = FormatDouble(cps);
        this.IntervalMsTextBox.Text = FormatDouble(interval);
        this.VariationTextBox.Text = FormatDouble(variation);
        this.DutyMinTextBox.Text = dutyMin.ToString(CultureInfo.CurrentCulture);
        this.DutyMaxTextBox.Text = dutyMax.ToString(CultureInfo.CurrentCulture);
        this.ClickLimitTextBox.Text = clickLimit.ToString(CultureInfo.CurrentCulture);
        this.TimeLimitSecondsTextBox.Text = timeLimitSeconds.ToString(CultureInfo.CurrentCulture);
        this.isSynchronizingRateFields = false;
    }

    private void SaveSettings() {
        ClickProfile profile = this.BuildProfile();

        int clickLimitValue = ParseIntOrDefault(this.ClickLimitTextBox.Text, 100);
        int timeLimitSeconds = ParseIntOrDefault(this.TimeLimitSecondsTextBox.Text, 30);

        AppSettings settings = new AppSettings {
            Profile = profile,
            IsClickLimitEnabled = this.EnableClickLimitCheckBox.IsChecked == true,
            ClickLimitValue = Math.Clamp(clickLimitValue, 1, 1_000_000),
            IsTimeLimitEnabled = this.EnableTimeLimitCheckBox.IsChecked == true,
            TimeLimitSeconds = Math.Clamp(timeLimitSeconds, 1, 86_400),
            Hotkey = new HotkeyConfig {
                Mode = this.hotkeyConfig.Mode,
                Modifiers = this.hotkeyConfig.Modifiers,
                VirtualKeyCode = this.hotkeyConfig.VirtualKeyCode
            }
        };

        SettingsStore.Save(settings);
    }

    private void LoadSettings() {
        AppSettings settings = SettingsStore.Load();
        ClickProfile profile = settings.Profile;

        this.ClicksPerSecondTextBox.Text =
            FormatDouble(Clamp(profile.ClicksPerSecond, MinimumClicksPerSecond, MaximumClicksPerSecond));
        this.VariationTextBox.Text = FormatDouble(Clamp(profile.SpeedVariationPercent, 0.0, 95.0));
        this.DutyMinTextBox.Text = Math.Clamp(profile.DutyCycleMinPercent, 1, 99).ToString(CultureInfo.CurrentCulture);
        this.DutyMaxTextBox.Text = Math.Clamp(profile.DutyCycleMaxPercent, 1, 99).ToString(CultureInfo.CurrentCulture);
        this.EnableClickLimitCheckBox.IsChecked = settings.IsClickLimitEnabled;
        this.ClickLimitTextBox.Text =
            Math.Clamp(settings.ClickLimitValue, 1, 1_000_000).ToString(CultureInfo.CurrentCulture);
        this.EnableTimeLimitCheckBox.IsChecked = settings.IsTimeLimitEnabled;
        this.TimeLimitSecondsTextBox.Text =
            Math.Clamp(settings.TimeLimitSeconds, 1, 86_400).ToString(CultureInfo.CurrentCulture);

        this.isSynchronizingRateFields = true;
        if (TryParseDouble(this.ClicksPerSecondTextBox.Text, out double cps)) {
            double interval = Clamp(1000.0 / cps, MinimumIntervalMilliseconds, MaximumIntervalMilliseconds);
            this.IntervalMsTextBox.Text = FormatDouble(interval);
        }

        this.isSynchronizingRateFields = false;

        this.SelectActionMode(profile.ActionType);
        this.SelectKeyboardKey(profile.VirtualKeyCode <= 0 ? 0x41 : profile.VirtualKeyCode);

        this.hotkeyConfig = settings.Hotkey ?? new HotkeyConfig();
        this.SelectHotkeyMode(this.hotkeyConfig.Mode);
        this.HotkeyDisplayBadge.Value = FormatHotkey(this.hotkeyConfig);
    }

    private void SelectActionMode(ActionType actionType) {
        OptionItem<ActionType>? item = this.actionModes.FirstOrDefault(x => x.Value == actionType);
        if (item is not null) {
            this.ActionTypeComboBox.SelectedItem = item;
        }
    }

    private void SelectHotkeyMode(HotkeyMode mode) {
        OptionItem<HotkeyMode>? item = this.hotkeyModes.FirstOrDefault(x => x.Value == mode);
        if (item is not null) {
            this.HotkeyModeComboBox.SelectedItem = item;
        }
    }

    private void SelectKeyboardKey(int virtualKeyCode) {
        KeyOption? option = this.keyboardKeys.FirstOrDefault(x => x.VirtualKeyCode == virtualKeyCode);
        if (option is null) {
            option = this.keyboardKeys.FirstOrDefault(static x => x.VirtualKeyCode == 0x41);
        }

        if (option is not null) {
            this.KeyboardKeyComboBox.SelectedItem = option;
        }
    }

    private static List<KeyOption> BuildKeyboardKeys() {
        HashSet<int> seenVirtualKeys = new HashSet<int>();
        List<KeyOption> options = new List<KeyOption>(180);

        foreach (Key key in Enum.GetValues<Key>()) {
            if (IsModifierKey(key)) {
                continue;
            }

            int vkCode;
            try {
                vkCode = KeyInterop.VirtualKeyFromKey(key);
            }
            catch {
                continue;
            }

            if (vkCode <= 0 || vkCode > 255) {
                continue;
            }

            if (!seenVirtualKeys.Add(vkCode)) {
                continue;
            }

            string label = $"{key} (VK 0x{vkCode:X2})";
            options.Add(new KeyOption(vkCode, label));
        }

        options.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Label, right.Label));
        return options;
    }

    private static string FormatHotkey(HotkeyConfig config) {
        StringBuilder builder = new StringBuilder(32);

        if (config.Modifiers.HasFlag(HotkeyModifiers.Control)) {
            builder.Append("Ctrl + ");
        }

        if (config.Modifiers.HasFlag(HotkeyModifiers.Shift)) {
            builder.Append("Shift + ");
        }

        if (config.Modifiers.HasFlag(HotkeyModifiers.Alt)) {
            builder.Append("Alt + ");
        }

        if (config.Modifiers.HasFlag(HotkeyModifiers.Win)) {
            builder.Append("Win + ");
        }

        builder.Append(GetKeyDisplayName(config.VirtualKeyCode));
        return builder.ToString();
    }

    private static string GetKeyDisplayName(int virtualKeyCode) {
        Key key = KeyInterop.KeyFromVirtualKey(virtualKeyCode);
        if (key == Key.None) {
            return $"VK 0x{virtualKeyCode:X2}";
        }

        return key.ToString();
    }

    private static HotkeyModifiers ReadModifiers() {
        HotkeyModifiers modifiers = HotkeyModifiers.None;

        ModifierKeys keyboardModifiers = Keyboard.Modifiers;
        if (keyboardModifiers.HasFlag(ModifierKeys.Control)) {
            modifiers |= HotkeyModifiers.Control;
        }

        if (keyboardModifiers.HasFlag(ModifierKeys.Shift)) {
            modifiers |= HotkeyModifiers.Shift;
        }

        if (keyboardModifiers.HasFlag(ModifierKeys.Alt)) {
            modifiers |= HotkeyModifiers.Alt;
        }

        if (IsWinDown()) {
            modifiers |= HotkeyModifiers.Win;
        }

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
        string text = (input ?? string.Empty).Trim();

        bool parsed = double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        if (parsed) {
            return true;
        }

        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static double ParseDoubleOrDefault(string? input, double fallback) {
        return TryParseDouble(input, out double value) ? value : fallback;
    }

    private static int ParseIntOrDefault(string? input, int fallback) {
        string text = (input ?? string.Empty).Trim();
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out int value)) {
            return value;
        }

        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)) {
            return value;
        }

        return fallback;
    }

    private static double Clamp(double value, double min, double max) {
        return Math.Min(max, Math.Max(min, value));
    }

    private static string FormatDouble(double value) {
        return Math.Round(value, 2).ToString("0.##", CultureInfo.CurrentCulture);
    }
}
