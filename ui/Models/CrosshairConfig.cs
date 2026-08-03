using Avalonia.Media;

namespace Blazma.Models;

/// <summary>Shape kinds. Must stay in sync with CrosshairType in src/Config.h.</summary>
public enum CrosshairType
{
    Cross,
    Dot,
    Circle,
    CrossDot,
    CircleDot
}

/// <summary>
/// The settings shared with the overlay through config.ini. Ranges match the
/// clamping the C++ side applies when it reads the file back.
/// </summary>
public sealed class CrosshairConfig
{
    public const int MinSize = 1, MaxSize = 100;
    public const int MinThickness = 1, MaxThickness = 20;
    public const int MinGap = 0, MaxGap = 50;
    public const int MinOpacity = 0, MaxOpacity = 255;

    public CrosshairType Type { get; set; } = CrosshairType.Cross;
    public Color Color { get; set; } = Color.FromRgb(0, 255, 0);
    public int Size { get; set; } = 20;
    public int Thickness { get; set; } = 2;
    public int Gap { get; set; }
    public bool Outline { get; set; }
    public int Opacity { get; set; } = 255;

    public CrosshairConfig Clone() => (CrosshairConfig)MemberwiseClone();

    public static string ToToken(CrosshairType type) => type switch
    {
        CrosshairType.Dot => "dot",
        CrosshairType.Circle => "circle",
        CrosshairType.CrossDot => "cross_dot",
        CrosshairType.CircleDot => "circle_dot",
        _ => "cross"
    };

    public static CrosshairType FromToken(string token, CrosshairType fallback) => token.Trim().ToLowerInvariant() switch
    {
        "cross" => CrosshairType.Cross,
        "dot" => CrosshairType.Dot,
        "circle" => CrosshairType.Circle,
        "cross_dot" => CrosshairType.CrossDot,
        "circle_dot" => CrosshairType.CircleDot,
        _ => fallback
    };
}
