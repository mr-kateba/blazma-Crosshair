#include "CrosshairRenderer.h"

#include <gdiplus.h>

using namespace Gdiplus;

namespace {

// Draws the four arms of a cross, leaving a centre gap.
void drawCross(Graphics& g, const Config& cfg, int cx, int cy, const Color& col, const Color& black) {
    int s = cfg.size;
    int t = cfg.thickness;

    int gap = cfg.gap;
    if (gap < 0) gap = 0;
    if (gap > s - 1) gap = s - 1;
    int gp = gap / 2;

    auto arms = [&](Pen& pen) {
        g.DrawLine(&pen, (REAL)(cx - s), (REAL)cy, (REAL)(cx - gp), (REAL)cy);
        g.DrawLine(&pen, (REAL)(cx + gp), (REAL)cy, (REAL)(cx + s), (REAL)cy);
        g.DrawLine(&pen, (REAL)cx, (REAL)(cy - s), (REAL)cx, (REAL)(cy - gp));
        g.DrawLine(&pen, (REAL)cx, (REAL)(cy + gp), (REAL)cx, (REAL)(cy + s));
    };

    if (cfg.outline) {
        Pen outPen(black, (REAL)(t + 2));
        outPen.SetStartCap(LineCapRound);
        outPen.SetEndCap(LineCapRound);
        arms(outPen);
    }

    Pen pen(col, (REAL)t);
    pen.SetStartCap(LineCapRound);
    pen.SetEndCap(LineCapRound);
    arms(pen);
}

// Draws a filled dot with a soft glow and a small specular highlight.
void drawDot(Graphics& g, const Config& cfg, int cx, int cy, int radius,
             const Color& col, const Color& black, int r, int gg, int b) {
    int t = cfg.thickness;

    int glow = 4 + (t / 2);
    if (glow > radius) glow = radius;
    if (glow < 1) glow = 1;

    GraphicsPath path;
    path.AddEllipse((REAL)(cx - radius - glow), (REAL)(cy - radius - glow),
                    (REAL)(2 * (radius + glow)), (REAL)(2 * (radius + glow)));
    PathGradientBrush glowBrush(&path);
    glowBrush.SetCenterColor(col);
    int count = 1;
    Color surround[] = { Color(0, r, gg, b) };
    glowBrush.SetSurroundColors(surround, &count);
    g.FillPath(&glowBrush, &path);

    if (cfg.outline) {
        int outW = (t < 4) ? 2 : (t / 2);
        int outR = radius + outW;
        SolidBrush outBrush(black);
        g.FillEllipse(&outBrush, (REAL)(cx - outR), (REAL)(cy - outR), (REAL)(2 * outR), (REAL)(2 * outR));
    }

    SolidBrush brush(col);
    g.FillEllipse(&brush, (REAL)(cx - radius), (REAL)(cy - radius), (REAL)(2 * radius), (REAL)(2 * radius));

    int hs = (radius > 4) ? (radius / 3) : 2;
    SolidBrush highlight(Color(130, 255, 255, 255));
    g.FillEllipse(&highlight, (REAL)(cx - hs * 0.6f), (REAL)(cy - radius + 2), (REAL)hs, (REAL)hs);
}

void drawCircle(Graphics& g, const Config& cfg, int cx, int cy, const Color& col, const Color& black) {
    int s = cfg.size;
    int t = cfg.thickness;

    if (cfg.outline) {
        Pen outPen(black, (REAL)(t + 2));
        g.DrawEllipse(&outPen, (REAL)(cx - s), (REAL)(cy - s), (REAL)(2 * s), (REAL)(2 * s));
    }
    Pen pen(col, (REAL)t);
    g.DrawEllipse(&pen, (REAL)(cx - s), (REAL)(cy - s), (REAL)(2 * s), (REAL)(2 * s));
}

void paint(Graphics& g, const Config& cfg, int w, int h) {
    g.SetSmoothingMode(SmoothingModeAntiAlias);
    g.SetPixelOffsetMode(PixelOffsetModeHalf);
    g.Clear(Color(0, 0, 0, 0));

    int cx = w / 2;
    int cy = h / 2;
    int r = GetRValue(cfg.color);
    int gg = GetGValue(cfg.color);
    int b = GetBValue(cfg.color);

    Color col(255, r, gg, b);
    Color black(255, 0, 0, 0);

    // Centre dot of the combined shapes tracks line thickness so it stays proportional.
    int centreDot = (cfg.thickness < 4) ? 2 : (cfg.thickness / 2);

    switch (cfg.type) {
        case CrosshairType::Cross:
            drawCross(g, cfg, cx, cy, col, black);
            break;
        case CrosshairType::Dot:
            drawDot(g, cfg, cx, cy, cfg.size, col, black, r, gg, b);
            break;
        case CrosshairType::Circle:
            drawCircle(g, cfg, cx, cy, col, black);
            break;
        case CrosshairType::CrossDot:
            drawCross(g, cfg, cx, cy, col, black);
            drawDot(g, cfg, cx, cy, centreDot, col, black, r, gg, b);
            break;
        case CrosshairType::CircleDot:
            drawCircle(g, cfg, cx, cy, col, black);
            drawDot(g, cfg, cx, cy, centreDot, col, black, r, gg, b);
            break;
    }
}

}  // namespace

int overlayWindowSize(const Config& cfg) {
    int size = 2 * (cfg.size + cfg.thickness + 8);
    return size < 10 ? 10 : size;
}

void renderCrosshair(HWND hwnd, const Config& cfg) {
    RECT rc;
    GetClientRect(hwnd, &rc);
    int w = rc.right - rc.left;
    int h = rc.bottom - rc.top;
    if (w <= 0 || h <= 0) return;

    HDC hdcScreen = GetDC(NULL);
    HDC hdcMem = CreateCompatibleDC(hdcScreen);

    BITMAPINFO bmi = {};
    bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
    bmi.bmiHeader.biWidth = w;
    bmi.bmiHeader.biHeight = -h;  // top-down DIB
    bmi.bmiHeader.biPlanes = 1;
    bmi.bmiHeader.biBitCount = 32;
    bmi.bmiHeader.biCompression = BI_RGB;

    void* bits = nullptr;
    HBITMAP hbm = CreateDIBSection(hdcMem, &bmi, DIB_RGB_COLORS, &bits, NULL, 0);
    if (!hbm || !bits) {
        if (hbm) DeleteObject(hbm);
        DeleteDC(hdcMem);
        ReleaseDC(NULL, hdcScreen);
        return;
    }

    HBITMAP oldBmp = (HBITMAP)SelectObject(hdcMem, hbm);
    ZeroMemory(bits, (size_t)w * h * 4);

    // Scoped so GDI+ finishes writing into the DIB before it is handed to the DWM.
    {
        Bitmap layer(w, h, w * 4, PixelFormat32bppPARGB, (BYTE*)bits);
        Graphics g(&layer);
        paint(g, cfg, w, h);
    }

    GetWindowRect(hwnd, &rc);
    POINT ptDst = { rc.left, rc.top };
    POINT ptSrc = { 0, 0 };
    SIZE size = { w, h };

    BLENDFUNCTION blend = {};
    blend.BlendOp = AC_SRC_OVER;
    blend.BlendFlags = 0;
    blend.SourceConstantAlpha = cfg.opacity;
    blend.AlphaFormat = AC_SRC_ALPHA;

    UpdateLayeredWindow(hwnd, NULL, &ptDst, &size, hdcMem, &ptSrc, 0, &blend, ULW_ALPHA);

    SelectObject(hdcMem, oldBmp);
    DeleteObject(hbm);
    DeleteDC(hdcMem);
    ReleaseDC(NULL, hdcScreen);
}
