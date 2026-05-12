using System.Runtime.InteropServices;

namespace mkAutoClicker;

public sealed class NativeInput {
    private const int InputMouse = 0;
    private const int InputKeyboard = 1;

    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp = 0x0004;
    private const uint MouseEventRightDown = 0x0008;
    private const uint MouseEventRightUp = 0x0010;
    private const uint MouseEventMiddleDown = 0x0020;
    private const uint MouseEventMiddleUp = 0x0040;

    private const uint KeyEventUp = 0x0002;

    public void SendDown(ActionType actionType, int virtualKeyCode) {
        var input = actionType switch {
            ActionType.MouseLeft => CreateMouseInput(MouseEventLeftDown),
            ActionType.MouseMiddle => CreateMouseInput(MouseEventMiddleDown),
            ActionType.MouseRight => CreateMouseInput(MouseEventRightDown),
            ActionType.KeyboardKey => CreateKeyboardInput((ushort)virtualKeyCode, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(actionType))
        };

        SendSingleInput(input);
    }

    public void SendUp(ActionType actionType, int virtualKeyCode) {
        var input = actionType switch {
            ActionType.MouseLeft => CreateMouseInput(MouseEventLeftUp),
            ActionType.MouseMiddle => CreateMouseInput(MouseEventMiddleUp),
            ActionType.MouseRight => CreateMouseInput(MouseEventRightUp),
            ActionType.KeyboardKey => CreateKeyboardInput((ushort)virtualKeyCode, KeyEventUp),
            _ => throw new ArgumentOutOfRangeException(nameof(actionType))
        };

        SendSingleInput(input);
    }

    private static void SendSingleInput(NativeInputData input) {
        var inputs = new[] { input };
        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeInputData>());
        if (sent != inputs.Length) {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"SendInput failed with Win32 error {error}.");
        }
    }

    private static NativeInputData CreateMouseInput(uint flags) {
        return new NativeInputData {
            Type = InputMouse,
            Data = new InputUnion {
                MouseInput = new MouseInput {
                    DwFlags = flags,
                    DwExtraInfo = IntPtr.Zero
                }
            }
        };
    }

    private static NativeInputData CreateKeyboardInput(ushort vk, uint flags) {
        return new NativeInputData {
            Type = InputKeyboard,
            Data = new InputUnion {
                KeyboardInput = new KeyboardInput {
                    WVk = vk,
                    DwFlags = flags,
                    DwExtraInfo = IntPtr.Zero
                }
            }
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeInputData {
        public int Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion {
        [FieldOffset(0)] public MouseInput MouseInput;

        [FieldOffset(0)] public KeyboardInput KeyboardInput;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint DwFlags;
        public uint Time;
        public IntPtr DwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput {
        public ushort WVk;
        public ushort WScan;
        public uint DwFlags;
        public uint Time;
        public IntPtr DwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint cInputs, NativeInputData[] pInputs, int cbSize);
}