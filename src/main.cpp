// Blazma - crosshair overlay for Windows 10 and Windows 11
//
// A lightweight always-on-top, click-through crosshair overlay drawn with
// Win32 + GDI+. This process is purely the renderer: it owns the layered
// window, the global hotkeys and config.ini. All settings UI lives in the
// separate Avalonia app (see ui/), which edits config.ini and then sends
// BLAZMA_CMD_RELOAD so the change appears instantly.

#ifndef UNICODE
#define UNICODE
#endif
#ifndef _UNICODE
#define _UNICODE
#endif

#define NOMINMAX
#include <windows.h>
#include <gdiplus.h>
#include <shellapi.h>

#include <string>

#include "Config.h"
#include "CrosshairRenderer.h"
#include "Ipc.h"

// ---------------------------------------------------------------------------
// State
// ---------------------------------------------------------------------------

static Config g_cfg;
static std::wstring g_configPath;
static bool g_visible = true;
static ULONG_PTR g_gdiplusToken = 0;
static UINT g_commandMessage = 0;

// Hotkey ids.
enum {
    HK_TOGGLE = 1,
    HK_EDIT_FILE = 2,
    HK_QUIT = 3,
    HK_SETTINGS = 4
};

// ---------------------------------------------------------------------------
// DPI and monitor placement
// ---------------------------------------------------------------------------

// Opt into per-monitor DPI awareness so monitor rects report real pixels.
// Without this Windows virtualizes the coordinates and then bitmap-stretches the
// layered window, which puts the crosshair off-centre and blurs it.
static void enableDpiAwareness() {
    HMODULE hUser = GetModuleHandleW(L"user32.dll");
    if (hUser) {
        typedef BOOL(WINAPI * SetCtxFn)(HANDLE);
        auto setCtx = (SetCtxFn)GetProcAddress(hUser, "SetProcessDpiAwarenessContext");
        if (setCtx && setCtx((HANDLE)-4)) return;  // PER_MONITOR_AWARE_V2
        if (setCtx && setCtx((HANDLE)-3)) return;  // PER_MONITOR_AWARE
    }
    SetProcessDPIAware();
}

// Pick the monitor the crosshair should sit on: the one holding the mouse
// cursor, falling back to the primary. On a multi-monitor setup this lets the
// overlay follow the display the game is actually running on.
static void getTargetMonitorRect(RECT* out) {
    POINT pt = { 0, 0 };
    GetCursorPos(&pt);
    HMONITOR hm = MonitorFromPoint(pt, MONITOR_DEFAULTTOPRIMARY);

    MONITORINFO mi = {};
    mi.cbSize = sizeof(mi);
    if (hm && GetMonitorInfoW(hm, &mi)) {
        *out = mi.rcMonitor;
        return;
    }

    out->left = 0;
    out->top = 0;
    out->right = GetSystemMetrics(SM_CXSCREEN);
    out->bottom = GetSystemMetrics(SM_CYSCREEN);
}

static void centreOverlay(HWND hwnd) {
    RECT mon;
    getTargetMonitorRect(&mon);

    int winSize = overlayWindowSize(g_cfg);
    int x = mon.left + ((mon.right - mon.left) - winSize) / 2;
    int y = mon.top + ((mon.bottom - mon.top) - winSize) / 2;
    SetWindowPos(hwnd, HWND_TOPMOST, x, y, winSize, winSize, SWP_NOACTIVATE);
}

static void refresh(HWND hwnd) {
    centreOverlay(hwnd);
    renderCrosshair(hwnd, g_cfg);
}

// ---------------------------------------------------------------------------
// Settings UI launch
// ---------------------------------------------------------------------------

static std::wstring exeDirectory() {
    wchar_t path[MAX_PATH];
    DWORD len = GetModuleFileNameW(NULL, path, MAX_PATH);
    if (len == 0) return L"";
    std::wstring s(path, len);
    size_t pos = s.find_last_of(L"\\/");
    return pos == std::wstring::npos ? L"" : s.substr(0, pos);
}

static bool fileExists(const std::wstring& path) {
    DWORD attrs = GetFileAttributesW(path.c_str());
    return attrs != INVALID_FILE_ATTRIBUTES && !(attrs & FILE_ATTRIBUTE_DIRECTORY);
}

// Opens the Avalonia settings app. Falls back to editing config.ini in Notepad
// so the overlay stays configurable even if the UI has not been built.
static void openSettings() {
    std::wstring dir = exeDirectory();
    const wchar_t* candidates[] = { L"\\Blazma.exe", L"\\ui\\Blazma.exe" };

    for (const wchar_t* rel : candidates) {
        std::wstring path = dir + rel;
        if (fileExists(path)) {
            ShellExecuteW(NULL, L"open", path.c_str(), NULL, dir.c_str(), SW_SHOWNORMAL);
            return;
        }
    }

    if (!g_configPath.empty()) {
        std::wstring params = L"\"" + g_configPath + L"\"";
        ShellExecuteW(NULL, L"open", L"notepad.exe", params.c_str(), NULL, SW_SHOWNORMAL);
    }
}

static void setVisible(HWND hwnd, bool visible) {
    g_visible = visible;
    ShowWindow(hwnd, visible ? SW_SHOWNOACTIVATE : SW_HIDE);
}

// ---------------------------------------------------------------------------
// Window procedure
// ---------------------------------------------------------------------------

static LRESULT CALLBACK WndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    // Commands posted by the settings UI.
    if (g_commandMessage != 0 && msg == g_commandMessage) {
        switch ((int)wParam) {
            case BLAZMA_CMD_RELOAD:
                loadConfig(g_configPath, g_cfg);
                refresh(hwnd);
                break;
            case BLAZMA_CMD_SHOW:
                setVisible(hwnd, true);
                break;
            case BLAZMA_CMD_HIDE:
                setVisible(hwnd, false);
                break;
            case BLAZMA_CMD_TOGGLE:
                setVisible(hwnd, !g_visible);
                break;
            case BLAZMA_CMD_EXIT:
                DestroyWindow(hwnd);
                break;
        }
        return 0;
    }

    if (msg == WM_HOTKEY) {
        switch (wParam) {
            case HK_TOGGLE:
                setVisible(hwnd, !g_visible);
                break;
            case HK_EDIT_FILE:
                if (!g_configPath.empty()) {
                    std::wstring params = L"\"" + g_configPath + L"\"";
                    ShellExecuteW(NULL, L"open", L"notepad.exe", params.c_str(), NULL, SW_SHOWNORMAL);
                }
                break;
            case HK_QUIT:
                DestroyWindow(hwnd);
                break;
            case HK_SETTINGS:
                openSettings();
                break;
        }
        return 0;
    }

    if (msg == WM_DISPLAYCHANGE) {
        // Resolution or monitor layout changed (a game switching mode, for
        // example), so the cached centre is stale.
        refresh(hwnd);
        return 0;
    }

    if (msg == WM_DESTROY) {
        PostQuitMessage(0);
        return 0;
    }

    return DefWindowProcW(hwnd, msg, wParam, lParam);
}

// ---------------------------------------------------------------------------
// Entry point
// ---------------------------------------------------------------------------

int WINAPI wWinMain(HINSTANCE hInstance, HINSTANCE, LPWSTR, int) {
    // Must run before any window is created.
    enableDpiAwareness();

    // A second instance would stack another topmost layered window on the first
    // and silently lose the hotkeys, which looks like the app freezing.
    HANDLE hMutex = CreateMutexW(NULL, TRUE, BLAZMA_MUTEX);
    if (hMutex && GetLastError() == ERROR_ALREADY_EXISTS) {
        MessageBoxW(NULL,
                    L"Blazma overlay is already running.\n"
                    L"Use Ctrl+Shift+M to open settings, or Ctrl+Shift+Q to close it.",
                    L"Blazma", MB_OK | MB_ICONINFORMATION);
        CloseHandle(hMutex);
        return 0;
    }

    Gdiplus::GdiplusStartupInput gdiInput;
    Gdiplus::GdiplusStartup(&g_gdiplusToken, &gdiInput, NULL);

    g_configPath = resolveConfigPath();
    if (!loadConfig(g_configPath, g_cfg)) {
        saveConfig(g_configPath, g_cfg);
    }

    g_commandMessage = RegisterWindowMessageW(BLAZMA_COMMAND_MESSAGE);

    WNDCLASSEXW wc = {};
    wc.cbSize = sizeof(WNDCLASSEXW);
    wc.lpfnWndProc = WndProc;
    wc.hInstance = hInstance;
    wc.hCursor = LoadCursor(NULL, IDC_ARROW);
    wc.lpszClassName = BLAZMA_WINDOW_CLASS;

    if (!RegisterClassExW(&wc)) {
        MessageBoxW(NULL, L"Failed to register window class.", L"Blazma", MB_OK | MB_ICONERROR);
        return 1;
    }

    RECT mon;
    getTargetMonitorRect(&mon);
    int winSize = overlayWindowSize(g_cfg);
    int x = mon.left + ((mon.right - mon.left) - winSize) / 2;
    int y = mon.top + ((mon.bottom - mon.top) - winSize) / 2;

    HWND hwnd = CreateWindowExW(
        WS_EX_LAYERED | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST | WS_EX_TRANSPARENT,
        BLAZMA_WINDOW_CLASS,
        L"Blazma",
        WS_POPUP,
        x, y, winSize, winSize,
        NULL, NULL, hInstance, NULL);

    if (!hwnd) {
        MessageBoxW(NULL, L"Failed to create overlay window.", L"Blazma", MB_OK | MB_ICONERROR);
        return 1;
    }

    ShowWindow(hwnd, SW_SHOWNOACTIVATE);
    renderCrosshair(hwnd, g_cfg);

    // All hotkeys are deliberately modified combinations. Registering a bare key
    // such as Insert or End claims it system-wide, so it never reaches the app
    // you are typing in and any stray press closes the overlay.
    const UINT mods = MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT;
    BOOL okToggle   = RegisterHotKey(hwnd, HK_TOGGLE, mods, 'C');
    BOOL okEditFile = RegisterHotKey(hwnd, HK_EDIT_FILE, mods, 'S');
    BOOL okQuit     = RegisterHotKey(hwnd, HK_QUIT, mods, 'Q');
    BOOL okSettings = RegisterHotKey(hwnd, HK_SETTINGS, mods, 'M');

    // A hotkey already owned by another app fails silently, leaving the overlay
    // with no visible way to control or close it.
    if (!okToggle || !okEditFile || !okQuit || !okSettings) {
        std::wstring warn = L"Some hotkeys are already in use by another program and will not work:\n\n";
        if (!okToggle)   warn += L"  Ctrl+Shift+C  (show / hide)\n";
        if (!okSettings) warn += L"  Ctrl+Shift+M  (settings)\n";
        if (!okEditFile) warn += L"  Ctrl+Shift+S  (edit config.ini)\n";
        if (!okQuit)     warn += L"  Ctrl+Shift+Q  (exit)\n";
        warn += L"\nYou can always close the overlay from Task Manager.";
        MessageBoxW(NULL, warn.c_str(), L"Blazma", MB_OK | MB_ICONWARNING);
    }

    MSG msg;
    while (GetMessageW(&msg, NULL, 0, 0)) {
        TranslateMessage(&msg);
        DispatchMessageW(&msg);
    }

    UnregisterHotKey(hwnd, HK_TOGGLE);
    UnregisterHotKey(hwnd, HK_EDIT_FILE);
    UnregisterHotKey(hwnd, HK_QUIT);
    UnregisterHotKey(hwnd, HK_SETTINGS);

    Gdiplus::GdiplusShutdown(g_gdiplusToken);
    if (hMutex) {
        ReleaseMutex(hMutex);
        CloseHandle(hMutex);
    }
    return (int)msg.wParam;
}
