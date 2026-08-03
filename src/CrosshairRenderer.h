// GDI+ drawing of the crosshair into a layered window.

#pragma once

#include <windows.h>

#include "Config.h"

// Side length in pixels of the square overlay window needed to hold the
// crosshair described by cfg, including margin for anti-aliasing.
int overlayWindowSize(const Config& cfg);

// Composites the crosshair and pushes it to the layered window.
void renderCrosshair(HWND hwnd, const Config& cfg);
