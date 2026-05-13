using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flow.Launcher.Plugin.Snippets.Util;

public class AutoPasteHelper
{
    /// <summary>
    /// 已弃用 (Deprecated) Windows 2000 以前即存在
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    /// <summary>
    /// 推荐使用 (Recommended)
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION u;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);


    // Ctrl Key code
    private const ushort VK_CONTROL = 0x11;

    // V key code
    private const ushort VK_V = 0x56;

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;


    private static void Native_SendCtrlV()
    {
        try
        {
            // Ctrl down
            keybd_event((byte)VK_CONTROL, 0, 0, UIntPtr.Zero);
            // V down
            keybd_event((byte)VK_V, 0, 0, UIntPtr.Zero);
            // V up
            keybd_event((byte)VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            // Ctrl up
            keybd_event((byte)VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
        catch (Exception ex)
        {
            InnerLogger.Logger.Error("Native_SendCtrlV failed", ex);
        }
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

            TrySendInputCtrlV();
        }
        catch (Exception ex)
        {
            InnerLogger.Logger.Error("PasteWhenFocusRestoredAsync", ex);

            // At minimum, the snippet is already in clipboard
            // Optionally show a notification that auto-paste failed
        }
    }

    public static async Task AutoPasteAfterHideWindowsAsync(PluginInitContext context, int extraDelayMs = 50)
    {
        try
        {
            if (context.API.IsMainWindowVisible())
            {
                await Task.Delay(extraDelayMs).ConfigureAwait(false);
            }

            TrySendInputCtrlV();
        }
        catch (Exception ex)
        {
            InnerLogger.Logger.Error("Snippets Paste", ex);
        }
    }

    public enum AutoPasteMethod
    {
        NativeSendCtrlV = 0,


        HideAndNativeSendCtrlV = 1,

        /// <summary>
        /// My Way
        /// </summary>
        HideFlowAndSendCtrlV = 2,
    }

    public static void AutoPasteAsync(PluginInitContext context, int autoPasteMethod, int delayMs)
    {
        switch (autoPasteMethod)
        {
            case (int)AutoPasteMethod.NativeSendCtrlV:
                Task.Run(() => { _ = PasteWhenFocusRestoredAsync(context, delayMs); });
                break;
            case (int)AutoPasteMethod.HideAndNativeSendCtrlV:
                context.API.HideMainWindow();
                Task.Run(() => { _ = AutoPasteAfterHideWindowsAsync(context, delayMs); });
                break;
            case (int)AutoPasteMethod.HideFlowAndSendCtrlV:
                context.API.HideMainWindow();
                Task.Run(() =>
                {
                    if (delayMs > 0)
                        Thread.Sleep(delayMs);
                    SendKeys.SendWait("^v");
                });
                break;
        }
    }
}