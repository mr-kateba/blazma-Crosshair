// Crosshair settings and their config.ini representation.

#pragma once

#include <windows.h>
#include <string>

// Shape kinds understood by the renderer. The string forms are what appear
// in config.ini and in the settings UI.
enum class CrosshairType {
    Cross,
    Dot,
    Circle,
    CrossDot,
    CircleDot
};

struct Config {
    CrosshairType type = CrosshairType::Cross;
    COLORREF color = RGB(0, 255, 0);
    int size = 20;         // radius / half line length
    int thickness = 2;     // line width
    int gap = 0;           // centre gap for cross shapes
    bool outline = false;  // dark outline for contrast on light backgrounds
    BYTE opacity = 255;    // 0-255
};

const wchar_t* toString(CrosshairType t);
CrosshairType parseType(const std::wstring& s, CrosshairType fallback);

COLORREF parseColor(const std::wstring& v);
std::wstring formatColor(COLORREF c);

// Full path of config.ini next to the running executable.
std::wstring resolveConfigPath();

bool loadConfig(const std::wstring& path, Config& out);
void saveConfig(const std::wstring& path, const Config& cfg);
