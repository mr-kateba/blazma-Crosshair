#include "Config.h"

#include "Ipc.h"

#include <cwctype>
#include <fstream>
#include <iomanip>
#include <sstream>

namespace {

std::wstring trim(const std::wstring& s) {
    size_t a = 0;
    while (a < s.size() && iswspace(s[a])) ++a;
    size_t b = s.size();
    while (b > a && iswspace(s[b - 1])) --b;
    return s.substr(a, b - a);
}

std::wstring lower(const std::wstring& s) {
    std::wstring r = s;
    for (auto& c : r) c = towlower(c);
    return r;
}

int parseInt(const std::wstring& v, int def, int lo, int hi) {
    try {
        int n = std::stoi(trim(v));
        if (n < lo) n = lo;
        if (n > hi) n = hi;
        return n;
    } catch (...) {
        return def;
    }
}

std::wstring exeDir() {
    wchar_t path[MAX_PATH];
    DWORD len = GetModuleFileNameW(NULL, path, MAX_PATH);
    if (len == 0) return L"";
    std::wstring s(path, len);
    size_t pos = s.find_last_of(L"\\/");
    return pos == std::wstring::npos ? L"" : s.substr(0, pos);
}

void writeFile(const std::wstring& path, const Config& c) {
    std::wofstream f(path.c_str());
    if (!f.is_open()) return;

    f << L"; Crosshair Overlay Settings\n";
    f << L"; type = cross | dot | circle | cross_dot | circle_dot\n";
    f << L"; color = 0xRRGGBB (e.g., 0x00FF00)\n";
    f << L"; size = radius or half line length (1-500)\n";
    f << L"; thickness = line width (1-100)\n";
    f << L"; gap = center gap for cross (0-50)\n";
    f << L"; outline = 0 or 1\n";
    f << L"; opacity = 0-255\n";
    f << L"type=" << toString(c.type) << L"\n";
    f << L"color=" << formatColor(c.color) << L"\n";
    f << L"size=" << c.size << L"\n";
    f << L"thickness=" << c.thickness << L"\n";
    f << L"gap=" << c.gap << L"\n";
    f << L"outline=" << (c.outline ? 1 : 0) << L"\n";
    f << L"opacity=" << (int)c.opacity << L"\n";
}

}  // namespace

const wchar_t* toString(CrosshairType t) {
    switch (t) {
        case CrosshairType::Dot:       return L"dot";
        case CrosshairType::Circle:    return L"circle";
        case CrosshairType::CrossDot:  return L"cross_dot";
        case CrosshairType::CircleDot: return L"circle_dot";
        case CrosshairType::Cross:
        default:                       return L"cross";
    }
}

CrosshairType parseType(const std::wstring& s, CrosshairType fallback) {
    std::wstring v = lower(trim(s));
    if (v == L"cross")      return CrosshairType::Cross;
    if (v == L"dot")        return CrosshairType::Dot;
    if (v == L"circle")     return CrosshairType::Circle;
    if (v == L"cross_dot")  return CrosshairType::CrossDot;
    if (v == L"circle_dot") return CrosshairType::CircleDot;
    return fallback;
}

COLORREF parseColor(const std::wstring& v) {
    std::wstring s = trim(v);
    if (s.size() > 1 && s[0] == L'#') {
        s = s.substr(1);
    } else if (s.size() > 2 && s[0] == L'0' && (s[1] == L'x' || s[1] == L'X')) {
        s = s.substr(2);
    }

    unsigned long n = 0x00FF00;  // green default on error
    try {
        if (!s.empty()) n = std::stoul(s, nullptr, 16);
    } catch (...) {
        n = 0x00FF00;
    }

    // The user writes 0xRRGGBB, but GDI stores COLORREF as 0x00BBGGRR.
    return RGB((int)((n >> 16) & 0xFF), (int)((n >> 8) & 0xFF), (int)(n & 0xFF));
}

std::wstring formatColor(COLORREF c) {
    int rgb = (GetRValue(c) << 16) | (GetGValue(c) << 8) | GetBValue(c);
    std::wostringstream oss;
    oss << L"0x" << std::hex << std::uppercase << std::setw(6) << std::setfill(L'0') << rgb;
    return oss.str();
}

std::wstring resolveConfigPath() {
    // Portable mode: a config.ini already sitting next to the exe wins, so a copy
    // on a USB stick carries its settings with it.
    std::wstring local = exeDir();
    if (!local.empty()) local += L"\\";
    local += L"config.ini";
    if (GetFileAttributesW(local.c_str()) != INVALID_FILE_ATTRIBUTES) return local;

    // Installed mode: an install directory such as Program Files or a Steam library
    // is not writable, so settings have to live under %APPDATA%.
    wchar_t appData[MAX_PATH];
    DWORD n = GetEnvironmentVariableW(L"APPDATA", appData, MAX_PATH);
    if (n == 0 || n >= MAX_PATH) return local;

    std::wstring dir = std::wstring(appData, n) + L"\\" + BLAZMA_APPDATA_FOLDER;
    CreateDirectoryW(dir.c_str(), NULL);
    return dir + L"\\config.ini";
}

bool loadConfig(const std::wstring& path, Config& out) {
    std::wifstream f(path.c_str());
    if (!f.is_open()) return false;

    std::wstring line;
    while (std::getline(f, line)) {
        std::wstring t = trim(line);
        if (t.empty() || t[0] == L';' || t[0] == L'#') continue;

        size_t eq = t.find(L'=');
        if (eq == std::wstring::npos) continue;

        std::wstring key = lower(trim(t.substr(0, eq)));
        std::wstring value = trim(t.substr(eq + 1));

        if (key == L"type") {
            out.type = parseType(value, out.type);
        } else if (key == L"color") {
            out.color = parseColor(value);
        } else if (key == L"size") {
            out.size = parseInt(value, 20, 1, 500);
        } else if (key == L"thickness") {
            out.thickness = parseInt(value, 2, 1, 100);
        } else if (key == L"gap") {
            out.gap = parseInt(value, 0, 0, 50);
        } else if (key == L"outline") {
            out.outline = (parseInt(value, 0, 0, 1) != 0);
        } else if (key == L"opacity") {
            out.opacity = (BYTE)parseInt(value, 255, 0, 255);
        }
    }
    return true;
}

void saveConfig(const std::wstring& path, const Config& cfg) {
    writeFile(path, cfg);
}
