using System.Runtime.InteropServices;
using System.Text;

namespace QFact.WE2Video;

internal static class WindowFinder
{
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint eventThread, uint eventTime);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    private const int SwRestore = 9;
    private const int SwShowNoActivate = 4;
    private const int GwlExStyle = -20;
    private const long WsExToolWindow = 0x00000080L;
    private const long WsExAppWindow = 0x00040000L;
    private const long WsExNoActivate = 0x08000000L;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoOwnerZOrder = 0x0200;
    private const uint SwpFrameChanged = 0x0020;
    private const uint SwpShowWindow = 0x0040;
    private const uint EventObjectCreate = 0x8000;
    private const uint EventObjectShow = 0x8002;
    private const uint WineventOutOfContext = 0x0000;
    private const int ObjidWindow = 0;
    private static readonly IntPtr HwndBottom = new(1);

    public static async Task<IntPtr> WaitForWindowAsync(string expectedTitle, TimeSpan timeout, bool activate = true, CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hwnd = FindByTitle(expectedTitle);
            if (hwnd != IntPtr.Zero)
            {
                if (activate)
                {
                    ShowWindow(hwnd, SwRestore);
                    SetForegroundWindow(hwnd);
                }
                return hwnd;
            }
            await Task.Delay(15, cancellationToken);
        }
        return IntPtr.Zero;
    }

    public static IntPtr GetForegroundWindowHandle() => GetForegroundWindow();

    public static IDisposable CreateBackgroundWindowGuard(string expectedTitle, int width, int height, IntPtr previousForeground)
        => new BackgroundWindowGuard(expectedTitle, width, height, previousForeground);

    public static void ConfigureBackgroundCaptureWindow(IntPtr hwnd, int width, int height, IntPtr previousForeground)
    {
        if (hwnd == IntPtr.Zero) return;
        try
        {
            var exStyle = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
            exStyle |= WsExToolWindow | WsExNoActivate;
            exStyle &= ~WsExAppWindow;
            SetWindowLongPtr(hwnd, GwlExStyle, new IntPtr(exStyle));

            ShowWindow(hwnd, SwShowNoActivate);

            // Keep a tiny sliver on the virtual desktop. Completely off-screen accelerated windows
            // can stop producing DWM frames on some GPU/driver combinations and WGC then records black.
            var desktop = System.Windows.Forms.SystemInformation.VirtualScreen;
            const int visibleSliver = 64;
            var x = Math.Max(desktop.Left, desktop.Right - visibleSliver);
            var y = Math.Max(desktop.Top, desktop.Bottom - visibleSliver);
            SetWindowPos(hwnd, HwndBottom, x, y, Math.Max(64, width), Math.Max(64, height),
                SwpNoActivate | SwpNoOwnerZOrder | SwpFrameChanged | SwpShowWindow);

            if (GetForegroundWindow() == hwnd && previousForeground != IntPtr.Zero && previousForeground != hwnd)
                SetForegroundWindow(previousForeground);
        }
        catch (Exception ex)
        {
            AppLogger.Warn("Background window configuration failed: " + ex.Message);
        }
    }

    public static void ConfigureCompatibilityCaptureWindow(IntPtr hwnd, int width, int height, IntPtr previousForeground)
    {
        if (hwnd == IntPtr.Zero) return;
        try
        {
            var exStyle = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
            exStyle |= WsExToolWindow | WsExNoActivate;
            exStyle &= ~WsExAppWindow;
            SetWindowLongPtr(hwnd, GwlExStyle, new IntPtr(exStyle));

            var desktop = System.Windows.Forms.SystemInformation.VirtualScreen;
            var x = desktop.Left + 8;
            var y = desktop.Top + 8;
            ShowWindow(hwnd, SwShowNoActivate);
            SetWindowPos(hwnd, HwndBottom, x, y, Math.Max(64, width), Math.Max(64, height),
                SwpNoActivate | SwpNoOwnerZOrder | SwpFrameChanged | SwpShowWindow);

            if (GetForegroundWindow() == hwnd && previousForeground != IntPtr.Zero && previousForeground != hwnd)
                SetForegroundWindow(previousForeground);
        }
        catch (Exception ex)
        {
            AppLogger.Warn("Compatibility window configuration failed: " + ex.Message);
        }
    }

    public static void ConfigureVisibleCaptureFallback(IntPtr hwnd, int width, int height)
    {
        if (hwnd == IntPtr.Zero) return;
        try
        {
            var exStyle = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
            exStyle &= ~WsExNoActivate;
            exStyle &= ~WsExToolWindow;
            exStyle |= WsExAppWindow;
            SetWindowLongPtr(hwnd, GwlExStyle, new IntPtr(exStyle));

            var desktop = System.Windows.Forms.SystemInformation.VirtualScreen;
            var x = desktop.Left + Math.Max(0, Math.Min(80, desktop.Width - Math.Max(64, width)));
            var y = desktop.Top + Math.Max(0, Math.Min(80, desktop.Height - Math.Max(64, height)));
            ShowWindow(hwnd, SwRestore);
            SetWindowPos(hwnd, IntPtr.Zero, x, y, Math.Max(64, width), Math.Max(64, height),
                SwpNoOwnerZOrder | SwpFrameChanged | SwpShowWindow);
            SetForegroundWindow(hwnd);
        }
        catch (Exception ex)
        {
            AppLogger.Warn("Visible capture fallback configuration failed: " + ex.Message);
        }
    }

    public static uint GetProcessId(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return 0;
        GetWindowThreadProcessId(hwnd, out var pid);
        return pid;
    }

    public static IntPtr FindByTitle(string expectedTitle)
    {
        IntPtr exact = IntPtr.Zero;
        IntPtr contains = IntPtr.Zero;
        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd)) return true;
            var title = ReadTitle(hWnd);
            if (title.Length == 0) return true;
            if (string.Equals(title, expectedTitle, StringComparison.Ordinal)) { exact = hWnd; return false; }
            if (contains == IntPtr.Zero && title.Contains(expectedTitle, StringComparison.Ordinal)) contains = hWnd;
            return true;
        }, IntPtr.Zero);
        return exact != IntPtr.Zero ? exact : contains;
    }

    private static string ReadTitle(IntPtr hwnd)
    {
        var len = GetWindowTextLength(hwnd);
        if (len <= 0) return string.Empty;
        var sb = new StringBuilder(len + 1);
        GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static IntPtr GetWindowLongPtr(IntPtr hwnd, int index)
        => IntPtr.Size == 8 ? GetWindowLongPtr64(hwnd, index) : new IntPtr(GetWindowLong32(hwnd, index));
    private static IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value)
        => IntPtr.Size == 8 ? SetWindowLongPtr64(hwnd, index, value) : new IntPtr(SetWindowLong32(hwnd, index, value.ToInt32()));

    private sealed class BackgroundWindowGuard : IDisposable
    {
        private readonly string _title;
        private readonly int _width;
        private readonly int _height;
        private readonly IntPtr _previousForeground;
        private readonly WinEventDelegate _callback;
        private IntPtr _hook;
        private bool _disposed;

        public BackgroundWindowGuard(string title, int width, int height, IntPtr previousForeground)
        {
            _title = title;
            _width = width;
            _height = height;
            _previousForeground = previousForeground;
            _callback = OnWinEvent;
            _hook = SetWinEventHook(EventObjectCreate, EventObjectShow, IntPtr.Zero, _callback, 0, 0, WineventOutOfContext);
            if (_hook == IntPtr.Zero) AppLogger.Warn("Could not install background window event guard; polling fallback will be used.");
        }

        private void OnWinEvent(IntPtr hook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint eventThread, uint eventTime)
        {
            if (_disposed || hwnd == IntPtr.Zero || idObject != ObjidWindow) return;
            try
            {
                var title = ReadTitle(hwnd);
                if (!string.Equals(title, _title, StringComparison.Ordinal) && !title.Contains(_title, StringComparison.Ordinal)) return;
                ConfigureBackgroundCaptureWindow(hwnd, _width, _height, _previousForeground);
            }
            catch { }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_hook != IntPtr.Zero)
            {
                UnhookWinEvent(_hook);
                _hook = IntPtr.Zero;
            }
            GC.KeepAlive(_callback);
        }
    }
}
