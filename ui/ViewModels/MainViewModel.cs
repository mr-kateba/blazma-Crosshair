using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using Blazma.Localization;
using Blazma.Models;
using Blazma.Services;

namespace Blazma.ViewModels;

/// <summary>
/// An entry in the shape picker. Identity is the shape alone: switching language
/// rebuilds this list with translated labels, and the ComboBox must still recognise
/// its current selection in the new list or it silently clears itself.
/// </summary>
public sealed record ShapeOption(CrosshairType Value, string Label)
{
    public bool Equals(ShapeOption? other) => other is not null && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
}

/// <summary>A preset paired with its name in the active language.</summary>
public sealed record PresetOption(CrosshairPreset Preset, string Name, string Description);

public sealed class MainViewModel : ViewModelBase
{
    // Long enough to coalesce a slider drag into a few writes, short enough that
    // the overlay still feels like it updates live.
    private static readonly TimeSpan ApplyDelay = TimeSpan.FromMilliseconds(120);
    private static readonly TimeSpan StatusPollInterval = TimeSpan.FromSeconds(1);

    private readonly ConfigService _configService;
    private readonly OverlayService _overlay;
    private readonly DispatcherTimer _applyTimer;
    private readonly DispatcherTimer _statusTimer;

    /// <summary>What the file said, before any control had a chance to write back.</summary>
    private CrosshairConfig _snapshot;

    /// <summary>Saving stays off until the window has settled. See <see cref="GoLive"/>.</summary>
    private bool _live;

    private CrosshairType _type = CrosshairType.Cross;
    private Color _color = Color.FromRgb(0, 255, 0);
    private int _size = 20;
    private int _thickness = 2;
    private int _gap;
    private bool _outline;
    private int _opacity = 255;
    private double _zoom = 1.0;
    private bool _isOverlayRunning;
    private Language _language = Language.English;

    public MainViewModel(ConfigService configService, OverlayService overlay)
    {
        _configService = configService;
        _overlay = overlay;

        ApplyPresetCommand = new RelayCommand(p => ApplyPreset((p as PresetOption)?.Preset));
        PickSwatchCommand = new RelayCommand(p => { if (p is Color c) Color = c; });
        ResetCommand = new RelayCommand(() => ApplyPreset(CrosshairPreset.All[0]));
        ToggleOverlayCommand = new RelayCommand(ToggleOverlay);
        StopOverlayCommand = new RelayCommand(StopOverlay, () => IsOverlayRunning);

        _applyTimer = new DispatcherTimer { Interval = ApplyDelay };
        _applyTimer.Tick += (_, _) => Flush();

        _statusTimer = new DispatcherTimer { Interval = StatusPollInterval };
        _statusTimer.Tick += (_, _) => RefreshStatus();

        SetLanguageCommand = new RelayCommand(p => { if (p is Language l) Language = l; });

        _snapshot = _configService.Load();
        Apply(_snapshot);

        _language = _configService.LoadLanguage(Language.English);
        ApplyLanguage();

        RefreshStatus();
        _statusTimer.Start();
    }

    // -----------------------------------------------------------------------
    // Language
    // -----------------------------------------------------------------------

    public Language Language
    {
        get => _language;
        set
        {
            if (SetField(ref _language, value))
            {
                ApplyLanguage();
                OnSettingChanged();
            }
        }
    }

    public Strings Text { get; private set; } = Strings.English;

    /// <summary>Arabic mirrors the whole window; the other two stay left to right.</summary>
    public FlowDirection FlowDirection =>
        Text.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public IReadOnlyList<Language> Languages { get; } = Enum.GetValues<Language>();

    public bool IsEnglish
    {
        get => Language == Language.English;
        set { if (value) Language = Language.English; }
    }

    public bool IsArabic
    {
        get => Language == Language.Arabic;
        set { if (value) Language = Language.Arabic; }
    }

    public bool IsChinese
    {
        get => Language == Language.Chinese;
        set { if (value) Language = Language.Chinese; }
    }

    public bool IsRussian
    {
        get => Language == Language.Russian;
        set { if (value) Language = Language.Russian; }
    }

    /// <summary>
    /// Retranslates the window in place. The fonts are pushed into the application
    /// resources because the styles reference them dynamically - Bahnschrift and
    /// Inter carry no Arabic or CJK glyphs, so the face has to change with the text.
    /// </summary>
    private void ApplyLanguage()
    {
        Text = Strings.For(_language);

        if (Application.Current is { } app)
        {
            app.Resources["TechFont"] = new FontFamily(Text.TechFont);
            app.Resources["BodyFont"] = new FontFamily(Text.BodyFont);
            app.Resources["MonoFont"] = new FontFamily(Text.MonoFont);
        }

        Shapes = Enum.GetValues<CrosshairType>()
            .Select(t => new ShapeOption(t, Text.ShapeName(t)))
            .ToList();

        Presets = CrosshairPreset.All
            .Select(p => new PresetOption(p, Text.PresetName(p.Id), Text.PresetDescription(p.Id)))
            .ToList();

        OnPropertyChanged(nameof(Text));
        OnPropertyChanged(nameof(FlowDirection));
        OnPropertyChanged(nameof(Shapes));
        OnPropertyChanged(nameof(Presets));
        OnPropertyChanged(nameof(SelectedShape));
        OnPropertyChanged(nameof(SubtensionText));
        OnPropertyChanged(nameof(OverlayStatusText));
        OnPropertyChanged(nameof(OverlayActionText));
        OnPropertyChanged(nameof(IsEnglish));
        OnPropertyChanged(nameof(IsArabic));
        OnPropertyChanged(nameof(IsChinese));
        OnPropertyChanged(nameof(IsRussian));
    }

    // -----------------------------------------------------------------------
    // Settings
    // -----------------------------------------------------------------------

    public CrosshairType Type
    {
        get => _type;
        set
        {
            if (SetField(ref _type, value))
            {
                OnPropertyChanged(nameof(SelectedShape));
                OnSettingChanged();
            }
        }
    }

    /// <summary>
    /// The shape picker binds to this rather than to <see cref="Type"/> directly.
    /// A ComboBox using SelectedValue pushes the enum's default back before its
    /// ItemsSource has resolved, which silently rewrote the shape on every open.
    /// Binding the item itself lets the null be ignored.
    /// </summary>
    public ShapeOption? SelectedShape
    {
        get => Shapes.FirstOrDefault(s => s.Value == Type);
        set { if (value is not null) Type = value.Value; }
    }

    public Color Color
    {
        get => _color;
        set
        {
            if (SetField(ref _color, value))
            {
                OnPropertyChanged(nameof(ColorHex));
                OnSettingChanged();
            }
        }
    }

    public int Size
    {
        get => _size;
        set
        {
            if (SetField(ref _size, Math.Clamp(value, CrosshairConfig.MinSize, CrosshairConfig.MaxSize)))
            {
                OnPropertyChanged(nameof(SubtensionText));
                OnSettingChanged();
            }
        }
    }

    public int Thickness
    {
        get => _thickness;
        set { if (SetField(ref _thickness, Math.Clamp(value, CrosshairConfig.MinThickness, CrosshairConfig.MaxThickness))) OnSettingChanged(); }
    }

    public int Gap
    {
        get => _gap;
        set { if (SetField(ref _gap, Math.Clamp(value, CrosshairConfig.MinGap, CrosshairConfig.MaxGap))) OnSettingChanged(); }
    }

    public bool Outline
    {
        get => _outline;
        set { if (SetField(ref _outline, value)) OnSettingChanged(); }
    }

    public int Opacity
    {
        get => _opacity;
        set
        {
            if (SetField(ref _opacity, Math.Clamp(value, CrosshairConfig.MinOpacity, CrosshairConfig.MaxOpacity)))
            {
                OnPropertyChanged(nameof(OpacityPercent));
                OnSettingChanged();
            }
        }
    }

    public string ColorHex => ConfigService.FormatColor(Color);

    public int OpacityPercent => (int)Math.Round(Opacity / 255.0 * 100);

    /// <summary>Overall width the crosshair occupies on screen.</summary>
    public string SubtensionText => $"{Size * 2} {Text.PixelUnit}";

    /// <summary>Preview magnification. Local to this window; never written to disk.</summary>
    public double Zoom
    {
        get => _zoom;
        set
        {
            if (SetField(ref _zoom, value))
            {
                OnPropertyChanged(nameof(IsZoom1));
                OnPropertyChanged(nameof(IsZoom2));
                OnPropertyChanged(nameof(IsZoom4));
            }
        }
    }

    // Bound to the zoom chips. Setting false is ignored so the group always keeps
    // exactly one selection.
    public bool IsZoom1
    {
        get => Math.Abs(Zoom - 1) < 0.01;
        set { if (value) Zoom = 1; }
    }

    public bool IsZoom2
    {
        get => Math.Abs(Zoom - 2) < 0.01;
        set { if (value) Zoom = 2; }
    }

    public bool IsZoom4
    {
        get => Math.Abs(Zoom - 4) < 0.01;
        set { if (value) Zoom = 4; }
    }

    // -----------------------------------------------------------------------
    // Option lists
    // -----------------------------------------------------------------------

    public IReadOnlyList<ShapeOption> Shapes { get; private set; } = [];

    public IReadOnlyList<PresetOption> Presets { get; private set; } = [];

    public IReadOnlyList<Color> Swatches { get; } = new[]
    {
        Color.FromRgb(0x00, 0xFF, 0x00), Color.FromRgb(0x00, 0xE5, 0xFF),
        Color.FromRgb(0xFF, 0x2D, 0x55), Color.FromRgb(0xFF, 0xD6, 0x0A),
        Color.FromRgb(0xFF, 0x00, 0xE5), Color.FromRgb(0xFF, 0xFF, 0xFF)
    };

    // -----------------------------------------------------------------------
    // Overlay status
    // -----------------------------------------------------------------------

    public bool IsOverlayRunning
    {
        get => _isOverlayRunning;
        private set
        {
            if (SetField(ref _isOverlayRunning, value))
            {
                OnPropertyChanged(nameof(OverlayStatusText));
                OnPropertyChanged(nameof(OverlayActionText));
                StopOverlayCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string OverlayStatusText => IsOverlayRunning ? Text.OverlayRunning : Text.OverlayStopped;

    public string OverlayActionText => IsOverlayRunning ? Text.RestartOverlay : Text.StartOverlay;

    // -----------------------------------------------------------------------
    // Commands
    // -----------------------------------------------------------------------

    public RelayCommand ApplyPresetCommand { get; }
    public RelayCommand PickSwatchCommand { get; }
    public RelayCommand SetLanguageCommand { get; }
    public RelayCommand ResetCommand { get; }
    public RelayCommand ToggleOverlayCommand { get; }
    public RelayCommand StopOverlayCommand { get; }

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------

    /// <summary>
    /// Puts the file's values back into the controls. Sliders, colour pickers and
    /// combo boxes push their own initial values back through their two-way
    /// bindings as they are laid out, which looks exactly like the user editing
    /// everything at once. The window calls this once layout has settled.
    /// </summary>
    public void RestoreFromFile() => Apply(_snapshot);

    /// <summary>
    /// Starts persisting changes. Driven by the first real click or keypress rather
    /// than a timer, because layout can keep writing back long after load - anything
    /// before genuine input is control noise, not an edit.
    /// </summary>
    public void GoLive()
    {
        if (_live) return;

        // Runs from a tunnelled input handler, so the control under the pointer has
        // not reacted yet. Re-seating the file's values here means saving starts
        // from a known-clean baseline no matter what layout wrote back beforehand.
        Apply(_snapshot);
        _live = true;
    }

    private void Apply(CrosshairConfig c)
    {
        _type = c.Type;
        _color = c.Color;
        _size = c.Size;
        _thickness = c.Thickness;
        _gap = c.Gap;
        _outline = c.Outline;
        _opacity = c.Opacity;

        OnPropertyChanged(nameof(Type));
        OnPropertyChanged(nameof(SelectedShape));
        OnPropertyChanged(nameof(Color));
        OnPropertyChanged(nameof(Size));
        OnPropertyChanged(nameof(Thickness));
        OnPropertyChanged(nameof(Gap));
        OnPropertyChanged(nameof(Outline));
        OnPropertyChanged(nameof(Opacity));
        OnPropertyChanged(nameof(ColorHex));
        OnPropertyChanged(nameof(OpacityPercent));
        OnPropertyChanged(nameof(SubtensionText));
    }

    private CrosshairConfig Current() => new()
    {
        Type = Type,
        Color = Color,
        Size = Size,
        Thickness = Thickness,
        Gap = Gap,
        Outline = Outline,
        Opacity = Opacity
    };

    private void ApplyPreset(CrosshairPreset? preset)
    {
        if (preset is null) return;

        Apply(preset.Config);
        OnSettingChanged();
    }

    /// <summary>Queues a debounced write so dragging a slider does not hammer the disk.</summary>
    private void OnSettingChanged()
    {
        if (!_live) return;
        _applyTimer.Stop();
        _applyTimer.Start();
    }

    /// <summary>Writes config.ini and tells the overlay to reload.</summary>
    public void Flush()
    {
        _applyTimer.Stop();
        if (!_live) return;

        _snapshot = Current();
        _configService.Save(_snapshot, Language);
        _overlay.Send(OverlayCommand.Reload);
    }

    private void ToggleOverlay()
    {
        Flush();

        if (IsOverlayRunning)
        {
            // Restart so a stale overlay picks up the current settings cleanly.
            _overlay.Send(OverlayCommand.Exit);
            DispatcherTimer.RunOnce(() => { _overlay.Launch(); RefreshStatus(); }, TimeSpan.FromMilliseconds(300));
        }
        else
        {
            _overlay.Launch();
        }

        RefreshStatus();
    }

    private void StopOverlay()
    {
        _overlay.Send(OverlayCommand.Exit);
        DispatcherTimer.RunOnce(RefreshStatus, TimeSpan.FromMilliseconds(300));
    }

    private void RefreshStatus() => IsOverlayRunning = _overlay.IsRunning;
}
