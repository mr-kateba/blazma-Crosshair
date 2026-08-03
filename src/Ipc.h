// Contract shared between the Blazma overlay and the Blazma settings UI.
// Both sides must agree on these values; keep them in sync with
// ui/Services/OverlayService.cs.

#pragma once

// Window class of the overlay. The UI locates the running overlay with
// FindWindowW(BLAZMA_WINDOW_CLASS, nullptr).
#define BLAZMA_WINDOW_CLASS L"BlazmaOverlay"

// Registered window message used to drive the overlay from the UI.
// RegisterWindowMessageW returns the same id in every process for this string.
#define BLAZMA_COMMAND_MESSAGE L"Blazma.Command"

// Only one overlay may run at a time.
#define BLAZMA_MUTEX L"Local\\BlazmaOverlaySingleInstance"

// Folder under %APPDATA% holding settings when the app is installed read-only.
#define BLAZMA_APPDATA_FOLDER L"Blazma"

// Sent as wParam of BLAZMA_COMMAND_MESSAGE.
enum BlazmaCommand {
    BLAZMA_CMD_RELOAD = 0,  // re-read config.ini and repaint
    BLAZMA_CMD_SHOW   = 1,
    BLAZMA_CMD_HIDE   = 2,
    BLAZMA_CMD_TOGGLE = 3,
    BLAZMA_CMD_EXIT   = 4
};
