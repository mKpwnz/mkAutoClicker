using System.Diagnostics;

namespace mkAutoClicker;

public enum ActionType {
    MouseLeft = 0,
    MouseMiddle = 1,
    MouseRight = 2,
    KeyboardKey = 3
}

public enum HotkeyMode {
    Toggle = 0,
    Hold = 1
}

[Flags]
public enum HotkeyModifiers {
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8
}

public enum StopReason {
    Cancelled = 0,
    ClickLimitReached = 1,
    TimeLimitReached = 2
}

public sealed class OptionItem<T> {
    public OptionItem(string label, T value) {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Value = value;
    }

    public string Label { get; }

    public T Value { get; }
}

public sealed class ClickProfile {
    public double ClicksPerSecond { get; set; } = 10.0;
    public double SpeedVariationPercent { get; set; }
    public int DutyCycleMinPercent { get; set; } = 35;
    public int DutyCycleMaxPercent { get; set; } = 65;
    public int? ClickLimit { get; set; }
    public TimeSpan? TimeLimit { get; set; }
    public ActionType ActionType { get; set; } = ActionType.MouseLeft;
    public int VirtualKeyCode { get; set; }
}

public sealed class HotkeyConfig {
    public HotkeyMode Mode { get; set; } = HotkeyMode.Toggle;
    public HotkeyModifiers Modifiers { get; set; } = HotkeyModifiers.Control | HotkeyModifiers.Alt;
    public int VirtualKeyCode { get; set; } = 0x75;
}

public sealed class AppSettings {
    public ClickProfile Profile { get; set; } = new();
    public bool IsClickLimitEnabled { get; set; }
    public int ClickLimitValue { get; set; } = 100;
    public bool IsTimeLimitEnabled { get; set; }
    public int TimeLimitSeconds { get; set; } = 30;
    public HotkeyConfig Hotkey { get; set; } = new();
    public string LanguageCode { get; set; } = "en";
}

public sealed class ClickProgress {
    public required int ClickCount { get; init; }
    public required TimeSpan Elapsed { get; init; }
}

public sealed class RunSummary {
    public required StopReason Reason { get; init; }
    public required int ClickCount { get; init; }
    public required TimeSpan Elapsed { get; init; }
}

public sealed class KeyOption {
    public KeyOption(int virtualKeyCode, string label, bool isGroupHeader = false) {
        VirtualKeyCode = virtualKeyCode;
        Label = label ?? throw new ArgumentNullException(nameof(label));
        IsGroupHeader = isGroupHeader;
    }

    public int VirtualKeyCode { get; }

    public string Label { get; }

    public bool IsGroupHeader { get; }

    public static KeyOption CreateGroupHeader(string label) {
        return new KeyOption(-1, label, true);
    }
}

public sealed class LanguageOption {
    public LanguageOption(string code, string label) {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Label = label ?? throw new ArgumentNullException(nameof(label));
    }

    public string Code { get; }

    public string Label { get; }
}

public static class Ensure {
    [Conditional("DEBUG")]
    public static void That(bool condition, string message) {
        if (!condition) throw new InvalidOperationException(message);
    }
}
