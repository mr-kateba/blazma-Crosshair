using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Blazma.Services;

/// <summary>Commands understood by the overlay. Mirrors CrosshairCommand in src/Ipc.h.</summary>
public enum OverlayCommand
{
    Reload = 0,
    Show = 1,
    Hide = 2,
    Toggle = 3,
    Exit = 4
}

/// <summary>
/// Talks to the running BlazmaOverlay.exe. The overlay is located by its window
/// class and driven with a registered window message, so no extra plumbing (pipes,
/// sockets, files beyond config.ini) is needed.
/// </summary>
public sealed partial class OverlayService
{
    // Must match src/Ipc.h.
    private const string WindowClass = "BlazmaOverlay";
    private const string CommandMessage = "Blazma.Command";
    private const string OverlayExeName = "BlazmaOverlay.exe";

    private readonly uint _commandId;

    public OverlayService()
    {
        _commandId = RegisterWindowMessageW(CommandMessage);
    }

    public bool IsRunning => FindOverlayWindow() != IntPtr.Zero;

    /// <summary>Tells a running overlay to re-read config.ini. No-op when it is not running.</summary>
    public bool Send(OverlayCommand command)
    {
        var hwnd = FindOverlayWindow();
        if (hwnd == IntPtr.Zero || _commandId == 0) return false;

        return PostMessageW(hwnd, _commandId, (IntPtr)(int)command, IntPtr.Zero);
    }

    /// <summary>Starts the overlay if it is not already running. Returns false when the exe is missing.</summary>
    public bool Launch()
    {
        if (IsRunning) return true;

        var exe = Path.Combine(AppContext.BaseDirectory, OverlayExeName);
        if (!File.Exists(exe)) return false;

        Process.Start(new ProcessStartInfo
        {
            FileName = exe,
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = true
        });

        return true;
    }

    private static IntPtr FindOverlayWindow() => FindWindowW(WindowClass, null);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr FindWindowW(string lpClassName, string? lpWindowName);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial uint RegisterWindowMessageW(string lpString);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PostMessageW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
}
