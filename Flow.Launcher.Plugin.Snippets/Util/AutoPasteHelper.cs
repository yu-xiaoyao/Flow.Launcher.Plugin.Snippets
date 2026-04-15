using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.Snippets.Util;

public class AutoPasteHelper
{
    // P/Invoke helpers to simulate Ctrl+V keypress and check foreground window
    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetFocus();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    private const uint WM_PASTE = 0x0302;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const byte VK_CONTROL = 0x11;
    private const byte VK_V = 0x56;


    /// <summary>
    /// Try to send WM_PASTE to the focused control of the foreground window.
    /// This bypasses UIPI restrictions that block keybd_event to elevated windows.
    /// Returns true if the message was sent successfully.
    /// </summary>
    private static bool TrySendWmPaste()
    {
        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero) return false;

        // Attach to the foreground window's thread to query its focused control
        var foregroundThread = GetWindowThreadProcessId(foregroundWindow, IntPtr.Zero);
        var currentThread = GetCurrentThreadId();

        var targetHandle = foregroundWindow;

        if (foregroundThread != currentThread)
        {
            if (AttachThreadInput(currentThread, foregroundThread, true))
            {
                var focused = GetFocus();
                AttachThreadInput(currentThread, foregroundThread, false);

                if (focused != IntPtr.Zero)
                    targetHandle = focused;
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

    private static void SendCtrlV()
    {
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

            // Try WM_PASTE first — it bypasses UIPI and works with elevated windows
            // Fall back to SendCtrlV for apps that don't handle WM_PASTE
            if (!TrySendWmPaste())
            {
                SendCtrlV();
            }
        }
        catch (Exception ex)
        {
            InnerLogger.Logger.Error("Snippets Paste", ex);

            // At minimum, the snippet is already in clipboard
            // Optionally show a notification that auto-paste failed
        }
    }
}