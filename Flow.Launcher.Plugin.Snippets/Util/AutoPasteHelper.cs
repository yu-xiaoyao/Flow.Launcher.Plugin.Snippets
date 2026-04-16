using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.Snippets.Util;

public class AutoPasteHelper
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetFocus();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    private const uint WM_PASTE = 0x0302;

    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_V = 0x56;

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    /// <summary>
    /// Use SendInput to simulate Ctrl+V.
    /// SendInput is the modern replacement for keybd_event and is more reliable.
    /// Returns true if all 4 input events were successfully injected.
    /// </summary>
    private static bool TrySendInputCtrlV()
    {
        var inputs = new INPUT[4];

        // Ctrl down
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].u.ki.wVk = VK_CONTROL;

        // V down
        inputs[1].type = INPUT_KEYBOARD;
        inputs[1].u.ki.wVk = VK_V;

        // V up
        inputs[2].type = INPUT_KEYBOARD;
        inputs[2].u.ki.wVk = VK_V;
        inputs[2].u.ki.dwFlags = KEYEVENTF_KEYUP;

        // Ctrl up
        inputs[3].type = INPUT_KEYBOARD;
        inputs[3].u.ki.wVk = VK_CONTROL;
        inputs[3].u.ki.dwFlags = KEYEVENTF_KEYUP;

        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        return sent == inputs.Length;
    }

    /// <summary>
    /// Try to send WM_PASTE to the focused control of the foreground window.
    /// WM_PASTE works for traditional Win32 edit controls (Notepad, etc.)
    /// but not for modern apps like VS Code that use custom rendering.
    /// </summary>
    private static bool TrySendWmPaste()
    {
        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero) return false;

        var foregroundThread = GetWindowThreadProcessId(foregroundWindow, IntPtr.Zero);
        var currentThread = GetCurrentThreadId();

        var targetHandle = foregroundWindow;

        if (foregroundThread != currentThread)
        {
            if (!AttachThreadInput(currentThread, foregroundThread, true))
                return false;

            try
            {
                var focused = GetFocus();
                if (focused != IntPtr.Zero)
                    targetHandle = focused;
            }
            finally
            {
                AttachThreadInput(currentThread, foregroundThread, false);
            }
        }
        else
        {
            var focused = GetFocus();
            if (focused != IntPtr.Zero)
                targetHandle = focused;
        }

        SendMessage(targetHandle, WM_PASTE, IntPtr.Zero, IntPtr.Zero);
        return true;
    }


    public static async Task PasteWhenFocusRestoredAsyncNew(PluginInitContext context, int extraDelayMs = 50)
    {
        try
        {
            // Wait until Flow Launcher main window is no longer visible
            const int timeoutMs = 2000;
            const int intervalMs = 100;
            var waited = 0;

            while (waited < timeoutMs && context.API.IsMainWindowVisible())
            {
                await Task.Delay(intervalMs).ConfigureAwait(false);
                waited += intervalMs;
            }

            // small extra delay to ensure target window is ready to accept input
            await Task.Delay(extraDelayMs).ConfigureAwait(false);

            // Strategy:
            // 1. SendInput Ctrl+V — works for most apps (VS Code, browsers, etc.)
            //    UIPI will block this if the target is elevated and we are not.
            // 2. WM_PASTE fallback — works for classic Win32 edit controls (Notepad, cmd)
            //    Also blocked by UIPI for elevated targets, but some controls allow it
            //    via ChangeWindowMessageFilter.
            if (!TrySendInputCtrlV())
            {
                TrySendWmPaste();
            }
        }
        catch (Exception ex)
        {
            InnerLogger.Logger.Error("Snippets Paste", ex);
        }
    }


    // 
    // old way
    // 

    // P/Invoke helpers to simulate Ctrl+V keypress and check foreground window
    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    public static async Task PasteWhenFocusRestoredAsync(PluginInitContext context, int extraDelayMs = 50)
    {
        try
        {
            // Wait until Flow Launcher main window is no longer visible
            const int timeoutMs = 2000; // max wait time for focus to switch
            const int intervalMs = 100;
            var waited = 0;

            while (waited < timeoutMs && context.API.IsMainWindowVisible())
            {
                await Task.Delay(intervalMs).ConfigureAwait(false);
                waited += intervalMs;
            }

            // small extra delay to ensure target window is ready to accept input
            await Task.Delay(extraDelayMs).ConfigureAwait(false);
            SendCtrlV();
        }
        catch (Exception ex)
        {
            InnerLogger.Logger.Error("Snippets Paste", ex);

            // At minimum, the snippet is already in clipboard
            // Optionally show a notification that auto-paste failed
        }
    }

    private static void SendCtrlV()
    {
        const byte VK_CONTROL = 0x11;
        const byte VK_V = 0x56;
        try
        {
            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            keybd_event(VK_V, 0, 0, UIntPtr.Zero);
            keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
        catch (Exception ex)
        {
            InnerLogger.Logger.Error("SendCtrlV failed", ex);
        }
    }
}