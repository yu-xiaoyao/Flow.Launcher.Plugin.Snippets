using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace Flow.Launcher.Plugin.Snippets.Util;

public class ClipboardUtils
{
    public enum CopyMethod
    {
        /// <summary>
        /// use Flow Launcher Copy
        /// </summary>
        Flow = 0,

        /// <summary>
        /// use dotnet Copy, default
        /// </summary>
        Dotnet = 1,

        /// <summary>
        /// use win32 dll copy
        /// </summary>
        Win32 = 2
    }

    public interface ICopyMethod
    {
        CopyMethod GetCopyMethod();

        void CopyToClipboard(string text);
    }

    public class DotnetCopyMethod : ICopyMethod
    {
        public CopyMethod GetCopyMethod()
        {
            return CopyMethod.Dotnet;
        }

        public void CopyToClipboard(string text)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                Clipboard.SetText(text);
            }
            else
            {
                var thread = new Thread(() => Clipboard.SetText(text));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            }
        }
    }

    public class FlowCopyMethod : ICopyMethod
    {
        private readonly PluginInitContext _context;
        private readonly bool _showDefaultNotification;

        public FlowCopyMethod(PluginInitContext context, bool showDefaultNotification = false)
        {
            _context = context;
            _showDefaultNotification = showDefaultNotification;
        }

        public CopyMethod GetCopyMethod()
        {
            return CopyMethod.Flow;
        }

        public void CopyToClipboard(string text)
        {
            _context.API.CopyToClipboard(text, showDefaultNotification: _showDefaultNotification);
        }
    }


    public class Win32CopyMethod : ICopyMethod
    {
        public CopyMethod GetCopyMethod()
        {
            return CopyMethod.Win32;
        }

        public void CopyToClipboard(string text)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                _setText(text);
            }
            else
            {
                // throw new InvalidOperationException("Clipboard operations should run in STA thread.");
                var thread = new Thread(() => _setText(text));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            }
        }

        private static void _setText(string text)
        {
            try
            {
                NativeSetText(text);
            }
            catch (Exception e)
            {
                // skip error
            }
        }
    }

    /// <summary>
    /// Copy Text to Clipboard
    /// </summary>
    /// <param name="text"></param>
    /// <param name="copyMethod"></param>
    public static void CopyToClipboard(string text, ICopyMethod copyMethod)
    {
        if (string.IsNullOrEmpty(text))
            return;
        copyMethod.CopyToClipboard(text);
    }


    public static void NativeSetText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        var hGlobal = IntPtr.Zero;
        try
        {
            // UTF-16 + \0
            var bytes = (text.Length + 1) * 2;

            hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes);

            if (hGlobal == IntPtr.Zero)
                throw new Win32Exception();

            var target = GlobalLock(hGlobal);

            if (target == IntPtr.Zero)
                throw new Win32Exception();

            try
            {
                Marshal.Copy(
                    text.ToCharArray(),
                    0,
                    target,
                    text.Length);

                Marshal.WriteInt16(
                    target,
                    text.Length * 2,
                    0);
            }
            finally
            {
                GlobalUnlock(hGlobal);
            }

            if (!OpenClipboard(IntPtr.Zero))
                throw new Win32Exception();

            try
            {
                if (!EmptyClipboard())
                    throw new Win32Exception();

                // 成功后系统接管内存所有权
                if (SetClipboardData(CF_UNICODETEXT, hGlobal) == IntPtr.Zero)
                    throw new Win32Exception();

                hGlobal = IntPtr.Zero;
            }
            finally
            {
                CloseClipboard();
            }
        }
        finally
        {
            if (hGlobal != IntPtr.Zero)
            {
                GlobalFree(hGlobal);
            }
        }
    }


    public const uint CF_UNICODETEXT = 13;
    public const uint GMEM_MOVEABLE = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GlobalFree(IntPtr hMem);
}