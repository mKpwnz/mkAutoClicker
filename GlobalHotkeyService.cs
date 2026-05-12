using System.Runtime.InteropServices;

namespace mkAutoClicker;

public sealed class GlobalHotkeyService
{
    public const int WmHotkey = 0x0312;

    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;

    private readonly int hotkeyId;

    private IntPtr hwnd;
    private bool isRegistered;
    private HotkeyConfig? currentConfig;

    public event EventHandler? Pressed;

    public GlobalHotkeyService(int hotkeyId = 1)
    {
        this.hotkeyId = hotkeyId;
    }

    public void AttachWindow(IntPtr windowHandle)
    {
        this.hwnd = windowHandle;
    }

    public bool Register(HotkeyConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        Ensure.That(this.hwnd != IntPtr.Zero, "Hotkey service requires a valid window handle.");

        this.Unregister();

        uint modifiers = ConvertModifiers(config.Modifiers);
        bool registered = RegisterHotKey(this.hwnd, this.hotkeyId, modifiers, (uint)config.VirtualKeyCode);
        if (!registered)
        {
            this.currentConfig = null;
            this.isRegistered = false;
            return false;
        }

        this.currentConfig = config;
        this.isRegistered = true;
        return true;
    }

    public void Unregister()
    {
        if (!this.isRegistered || this.hwnd == IntPtr.Zero)
        {
            return;
        }

        _ = UnregisterHotKey(this.hwnd, this.hotkeyId);
        this.isRegistered = false;
    }

    public void ProcessWindowMessage(int message, IntPtr wParam)
    {
        if (!this.isRegistered)
        {
            return;
        }

        if (message != WmHotkey)
        {
            return;
        }

        if (wParam.ToInt32() != this.hotkeyId)
        {
            return;
        }

        this.Pressed?.Invoke(this, EventArgs.Empty);
    }

    public bool IsCurrentHotkeyPressed()
    {
        HotkeyConfig? config = this.currentConfig;
        if (config is null)
        {
            return false;
        }

        if (!IsVirtualKeyPressed(config.VirtualKeyCode))
        {
            return false;
        }

        if (config.Modifiers.HasFlag(HotkeyModifiers.Control) && !IsVirtualKeyPressed(0x11))
        {
            return false;
        }

        if (config.Modifiers.HasFlag(HotkeyModifiers.Shift) && !IsVirtualKeyPressed(0x10))
        {
            return false;
        }

        if (config.Modifiers.HasFlag(HotkeyModifiers.Alt) && !IsVirtualKeyPressed(0x12))
        {
            return false;
        }

        if (config.Modifiers.HasFlag(HotkeyModifiers.Win) && !IsWinPressed())
        {
            return false;
        }

        return true;
    }

    private static uint ConvertModifiers(HotkeyModifiers modifiers)
    {
        uint value = 0;
        if (modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            value |= ModAlt;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Control))
        {
            value |= ModControl;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            value |= ModShift;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Win))
        {
            value |= ModWin;
        }

        return value;
    }

    private static bool IsWinPressed()
    {
        return IsVirtualKeyPressed(0x5B) || IsVirtualKeyPressed(0x5C);
    }

    private static bool IsVirtualKeyPressed(int virtualKeyCode)
    {
        short state = GetAsyncKeyState(virtualKeyCode);
        return (state & 0x8000) != 0;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}
