using Avalonia.Media;

namespace Blazma.Models;

public enum PresetId
{
    Classic,
    Precision,
    MicroDot,
    Ring,
    Sniper,
    Neon
}

/// <summary>
/// A named starting point the user can apply with one click. The preset carries no
/// text of its own - the name and description come from the active language.
/// </summary>
public sealed record CrosshairPreset(PresetId Id, CrosshairConfig Config)
{
    public static IReadOnlyList<CrosshairPreset> All { get; } = new[]
    {
        new CrosshairPreset(PresetId.Classic, new CrosshairConfig
        {
            Type = CrosshairType.Cross,
            Color = Color.FromRgb(0x00, 0xFF, 0x00),
            Size = 12, Thickness = 2, Gap = 4, Outline = false, Opacity = 255
        }),

        new CrosshairPreset(PresetId.Precision, new CrosshairConfig
        {
            Type = CrosshairType.CrossDot,
            Color = Color.FromRgb(0x00, 0xE5, 0xFF),
            Size = 10, Thickness = 1, Gap = 6, Outline = true, Opacity = 235
        }),

        new CrosshairPreset(PresetId.MicroDot, new CrosshairConfig
        {
            Type = CrosshairType.Dot,
            Color = Color.FromRgb(0xFF, 0x2D, 0x55),
            Size = 3, Thickness = 2, Gap = 0, Outline = true, Opacity = 255
        }),

        new CrosshairPreset(PresetId.Ring, new CrosshairConfig
        {
            Type = CrosshairType.CircleDot,
            Color = Color.FromRgb(0xFF, 0xD6, 0x0A),
            Size = 16, Thickness = 2, Gap = 0, Outline = true, Opacity = 220
        }),

        new CrosshairPreset(PresetId.Sniper, new CrosshairConfig
        {
            Type = CrosshairType.Cross,
            Color = Color.FromRgb(0xFF, 0xFF, 0xFF),
            Size = 26, Thickness = 1, Gap = 18, Outline = true, Opacity = 200
        }),

        new CrosshairPreset(PresetId.Neon, new CrosshairConfig
        {
            Type = CrosshairType.CrossDot,
            Color = Color.FromRgb(0xFF, 0x00, 0xE5),
            Size = 14, Thickness = 3, Gap = 5, Outline = true, Opacity = 255
        })
    };
}
