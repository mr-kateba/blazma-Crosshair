using System.Globalization;
using System.Text;
using Avalonia.Media;
using Blazma.Localization;
using Blazma.Models;

namespace Blazma.Services;

/// <summary>
/// Reads and writes config.ini in exactly the format src/Config.cpp expects.
/// The file is the single source of truth shared with the overlay process.
/// </summary>
public sealed class ConfigService
{
    public string ConfigPath { get; }

    public ConfigService(string? path = null)
    {
        ConfigPath = path ?? ResolveConfigPath();
    }

    /// <summary>
    /// Mirrors resolveConfigPath() in src/Config.cpp - both processes must land on
    /// the same file. An install directory such as Program Files or a Steam library
    /// is not writable, so settings go to %APPDATA% unless a config.ini is already
    /// sitting next to the executable (portable mode).
    /// </summary>
    public static string ResolveConfigPath()
    {
        var portable = Path.Combine(AppContext.BaseDirectory, "config.ini");
        if (File.Exists(portable)) return portable;

        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Blazma");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "config.ini");
    }

    public CrosshairConfig Load()
    {
        var config = new CrosshairConfig();
        if (!File.Exists(ConfigPath)) return config;

        foreach (var raw in File.ReadAllLines(ConfigPath))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;

            var eq = line.IndexOf('=');
            if (eq <= 0) continue;

            var key = line[..eq].Trim().ToLowerInvariant();
            var value = line[(eq + 1)..].Trim();

            switch (key)
            {
                case "type":
                    config.Type = CrosshairConfig.FromToken(value, config.Type);
                    break;
                case "color":
                    config.Color = ParseColor(value, config.Color);
                    break;
                case "size":
                    config.Size = ParseInt(value, config.Size, CrosshairConfig.MinSize, CrosshairConfig.MaxSize);
                    break;
                case "thickness":
                    config.Thickness = ParseInt(value, config.Thickness, CrosshairConfig.MinThickness, CrosshairConfig.MaxThickness);
                    break;
                case "gap":
                    config.Gap = ParseInt(value, config.Gap, CrosshairConfig.MinGap, CrosshairConfig.MaxGap);
                    break;
                case "outline":
                    config.Outline = ParseInt(value, 0, 0, 1) != 0;
                    break;
                case "opacity":
                    config.Opacity = ParseInt(value, config.Opacity, CrosshairConfig.MinOpacity, CrosshairConfig.MaxOpacity);
                    break;
            }
        }

        return config;
    }

    /// <summary>
    /// The UI language lives alongside the crosshair settings. It is a UI concern the
    /// overlay knows nothing about, but src/Config.cpp skips keys it does not
    /// recognise, so one shared file stays the whole contract.
    /// </summary>
    public Language LoadLanguage(Language fallback)
    {
        if (!File.Exists(ConfigPath)) return fallback;

        foreach (var raw in File.ReadAllLines(ConfigPath))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;

            var eq = line.IndexOf('=');
            if (eq <= 0) continue;

            if (line[..eq].Trim().Equals("language", StringComparison.OrdinalIgnoreCase))
                return LanguageCodes.FromCode(line[(eq + 1)..], fallback);
        }

        return fallback;
    }

    public void Save(CrosshairConfig config, Language language)
    {
        var sb = new StringBuilder();
        sb.AppendLine("; Blazma settings");
        sb.AppendLine("; type = cross | dot | circle | cross_dot | circle_dot");
        sb.AppendLine("; color = 0xRRGGBB (e.g., 0x00FF00)");
        sb.AppendLine("; size = radius or half line length (1-500)");
        sb.AppendLine("; thickness = line width (1-100)");
        sb.AppendLine("; gap = center gap for cross (0-50)");
        sb.AppendLine("; outline = 0 or 1");
        sb.AppendLine("; opacity = 0-255");
        sb.AppendLine("; language = en | ru | ar | zh   (settings UI only, ignored by the overlay)");
        sb.AppendLine($"type={CrosshairConfig.ToToken(config.Type)}");
        sb.AppendLine($"color={FormatColor(config.Color)}");
        sb.AppendLine($"size={config.Size}");
        sb.AppendLine($"thickness={config.Thickness}");
        sb.AppendLine($"gap={config.Gap}");
        sb.AppendLine($"outline={(config.Outline ? 1 : 0)}");
        sb.AppendLine($"opacity={config.Opacity}");
        sb.AppendLine($"language={LanguageCodes.ToCode(language)}");

        File.WriteAllText(ConfigPath, sb.ToString());
    }

    public static string FormatColor(Color c) =>
        $"0x{c.R:X2}{c.G:X2}{c.B:X2}";

    private static Color ParseColor(string value, Color fallback)
    {
        var s = value.Trim();
        if (s.StartsWith('#')) s = s[1..];
        else if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s[2..];

        if (int.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var n))
            return Color.FromRgb((byte)((n >> 16) & 0xFF), (byte)((n >> 8) & 0xFF), (byte)(n & 0xFF));

        return fallback;
    }

    private static int ParseInt(string value, int fallback, int lo, int hi) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
            ? Math.Clamp(n, lo, hi)
            : fallback;
}
