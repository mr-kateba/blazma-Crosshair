using Blazma.Models;

namespace Blazma.Localization;

/// <summary>
/// Every piece of user-facing text, in one object per language. Swapping the whole
/// object and re-raising a single property is enough to retranslate the window.
///
/// Fonts travel with the language: Bahnschrift carries no Arabic or CJK glyphs, so
/// those scripts fall back to faces that Windows actually ships with coverage for.
/// </summary>
public sealed class Strings
{
    public required Language Language { get; init; }
    public required bool IsRightToLeft { get; init; }

    public required string TechFont { get; init; }
    public required string BodyFont { get; init; }
    public required string MonoFont { get; init; }

    // Section headings
    public required string Presets { get; init; }
    public required string Form { get; init; }
    public required string Colour { get; init; }
    public required string Overlay { get; init; }
    public required string PreviewScale { get; init; }
    public required string LanguageLabel { get; init; }

    // Controls
    public required string Shape { get; init; }
    public required string Size { get; init; }
    public required string Thickness { get; init; }
    public required string CentreGap { get; init; }
    public required string DarkOutline { get; init; }
    public required string Reticle { get; init; }
    public required string Opacity { get; init; }
    public required string Subtension { get; init; }
    public required string PixelUnit { get; init; }
    public required string On { get; init; }
    public required string Off { get; init; }
    public required string CustomColour { get; init; }

    // Overlay control
    public required string OverlayRunning { get; init; }
    public required string OverlayStopped { get; init; }
    public required string StartOverlay { get; init; }
    public required string RestartOverlay { get; init; }
    public required string Stop { get; init; }
    public required string Reset { get; init; }
    public required string Hint { get; init; }

    public required Func<CrosshairType, string> ShapeName { get; init; }
    public required Func<PresetId, string> PresetName { get; init; }
    public required Func<PresetId, string> PresetDescription { get; init; }

    public static Strings For(Language language) => language switch
    {
        Language.Arabic => Arabic,
        Language.Chinese => Chinese,
        Language.Russian => Russian,
        _ => English
    };

    // -----------------------------------------------------------------------

    public static readonly Strings English = new()
    {
        Language = Language.English,
        IsRightToLeft = false,
        TechFont = "Bahnschrift,Segoe UI,sans-serif",
        BodyFont = "Inter,Segoe UI,sans-serif",
        MonoFont = "Cascadia Mono,Consolas,monospace",

        Presets = "PRESETS",
        Form = "FORM",
        Colour = "COLOUR",
        Overlay = "OVERLAY",
        PreviewScale = "PREVIEW SCALE",
        LanguageLabel = "LANGUAGE",

        Shape = "Shape",
        Size = "Size",
        Thickness = "Thickness",
        CentreGap = "Centre gap",
        DarkOutline = "Dark outline",
        Reticle = "Reticle",
        Opacity = "Opacity",
        Subtension = "Subtension",
        PixelUnit = "PX",
        On = "On",
        Off = "Off",
        CustomColour = "Custom colour",

        OverlayRunning = "Overlay running",
        OverlayStopped = "Overlay stopped",
        StartOverlay = "Start overlay",
        RestartOverlay = "Restart",
        Stop = "Stop",
        Reset = "Reset",
        Hint = "Changes save and apply as you make them. In game: Ctrl+Shift+C hides the crosshair, "
             + "Ctrl+Shift+M reopens this window, Ctrl+Shift+Q closes the overlay.",

        ShapeName = t => t switch
        {
            CrosshairType.Dot => "Dot",
            CrosshairType.Circle => "Circle",
            CrosshairType.CrossDot => "Cross + Dot",
            CrosshairType.CircleDot => "Circle + Dot",
            _ => "Cross"
        },
        PresetName = p => p switch
        {
            PresetId.Precision => "Precision",
            PresetId.MicroDot => "Micro Dot",
            PresetId.Ring => "Ring",
            PresetId.Sniper => "Sniper",
            PresetId.Neon => "Neon",
            _ => "Classic"
        },
        PresetDescription = p => p switch
        {
            PresetId.Precision => "Thin cross with centre dot",
            PresetId.MicroDot => "Minimal single dot",
            PresetId.Ring => "Circle with centre dot",
            PresetId.Sniper => "Wide arms, open centre",
            PresetId.Neon => "Bold high-contrast magenta",
            _ => "Balanced green cross"
        }
    };

    // -----------------------------------------------------------------------

    public static readonly Strings Arabic = new()
    {
        Language = Language.Arabic,
        IsRightToLeft = true,
        // Bahnschrift has no Arabic coverage; Segoe UI ships with it on Windows.
        TechFont = "Segoe UI,Tahoma,sans-serif",
        BodyFont = "Segoe UI,Tahoma,sans-serif",
        MonoFont = "Cascadia Mono,Consolas,monospace",

        Presets = "الأنماط الجاهزة",
        Form = "الشكل",
        Colour = "اللون",
        Overlay = "الطبقة",
        PreviewScale = "تكبير المعاينة",
        LanguageLabel = "اللغة",

        Shape = "النوع",
        Size = "الحجم",
        Thickness = "السماكة",
        CentreGap = "فراغ المنتصف",
        DarkOutline = "حدود داكنة",
        Reticle = "لون الكروسهير",
        Opacity = "الشفافية",
        Subtension = "العرض الكلي",
        PixelUnit = "بكسل",
        On = "مفعّل",
        Off = "معطّل",
        CustomColour = "لون مخصص",

        OverlayRunning = "الطبقة تعمل",
        OverlayStopped = "الطبقة متوقفة",
        StartOverlay = "تشغيل الطبقة",
        RestartOverlay = "إعادة تشغيل",
        Stop = "إيقاف",
        Reset = "استعادة",
        Hint = "تُحفظ التغييرات وتُطبَّق فورًا. داخل اللعبة: Ctrl+Shift+C لإخفاء الكروسهير، "
             + "Ctrl+Shift+M لفتح هذه النافذة، Ctrl+Shift+Q لإغلاق الطبقة.",

        ShapeName = t => t switch
        {
            CrosshairType.Dot => "نقطة",
            CrosshairType.Circle => "دائرة",
            CrosshairType.CrossDot => "صليب + نقطة",
            CrosshairType.CircleDot => "دائرة + نقطة",
            _ => "صليب"
        },
        PresetName = p => p switch
        {
            PresetId.Precision => "دقيق",
            PresetId.MicroDot => "نقطة صغيرة",
            PresetId.Ring => "حلقة",
            PresetId.Sniper => "قنص",
            PresetId.Neon => "نيون",
            _ => "كلاسيكي"
        },
        PresetDescription = p => p switch
        {
            PresetId.Precision => "صليب رفيع مع نقطة في المنتصف",
            PresetId.MicroDot => "نقطة مفردة بالحد الأدنى",
            PresetId.Ring => "دائرة مع نقطة في المنتصف",
            PresetId.Sniper => "أذرع واسعة ومركز مفتوح",
            PresetId.Neon => "ماجنتا عريض عالي التباين",
            _ => "صليب أخضر متوازن"
        }
    };

    // -----------------------------------------------------------------------

    public static readonly Strings Russian = new()
    {
        Language = Language.Russian,
        IsRightToLeft = false,
        // Bahnschrift covers Cyrillic, so the technical voice carries over intact.
        TechFont = "Bahnschrift,Segoe UI,sans-serif",
        BodyFont = "Inter,Segoe UI,sans-serif",
        MonoFont = "Cascadia Mono,Consolas,monospace",

        Presets = "ПРЕСЕТЫ",
        Form = "ФОРМА",
        Colour = "ЦВЕТ",
        Overlay = "ОВЕРЛЕЙ",
        PreviewScale = "МАСШТАБ ПРОСМОТРА",
        LanguageLabel = "ЯЗЫК",

        Shape = "Тип",
        Size = "Размер",
        Thickness = "Толщина",
        CentreGap = "Зазор в центре",
        DarkOutline = "Тёмный контур",
        Reticle = "Цвет прицела",
        Opacity = "Непрозрачность",
        Subtension = "Общая ширина",
        PixelUnit = "ПКС",
        On = "Вкл",
        Off = "Выкл",
        CustomColour = "Свой цвет",

        OverlayRunning = "Оверлей работает",
        OverlayStopped = "Оверлей остановлен",
        StartOverlay = "Запустить оверлей",
        RestartOverlay = "Перезапустить",
        Stop = "Остановить",
        Reset = "Сброс",
        Hint = "Изменения сохраняются и применяются сразу. В игре: Ctrl+Shift+C скрывает прицел, "
             + "Ctrl+Shift+M открывает это окно, Ctrl+Shift+Q закрывает оверлей.",

        ShapeName = t => t switch
        {
            CrosshairType.Dot => "Точка",
            CrosshairType.Circle => "Круг",
            CrosshairType.CrossDot => "Крест + точка",
            CrosshairType.CircleDot => "Круг + точка",
            _ => "Крест"
        },
        PresetName = p => p switch
        {
            PresetId.Precision => "Точность",
            PresetId.MicroDot => "Микроточка",
            PresetId.Ring => "Кольцо",
            PresetId.Sniper => "Снайпер",
            PresetId.Neon => "Неон",
            _ => "Классика"
        },
        PresetDescription = p => p switch
        {
            PresetId.Precision => "Тонкий крест с точкой в центре",
            PresetId.MicroDot => "Минимальная одиночная точка",
            PresetId.Ring => "Круг с точкой в центре",
            PresetId.Sniper => "Широкие лучи, открытый центр",
            PresetId.Neon => "Яркая контрастная пурпурная",
            _ => "Сбалансированный зелёный крест"
        }
    };

    // -----------------------------------------------------------------------

    public static readonly Strings Chinese = new()
    {
        Language = Language.Chinese,
        IsRightToLeft = false,
        // Bahnschrift and Inter carry no CJK glyphs.
        TechFont = "Microsoft YaHei UI,Microsoft YaHei,Segoe UI,sans-serif",
        BodyFont = "Microsoft YaHei UI,Microsoft YaHei,Segoe UI,sans-serif",
        MonoFont = "Cascadia Mono,Consolas,monospace",

        Presets = "预设",
        Form = "形状",
        Colour = "颜色",
        Overlay = "准星层",
        PreviewScale = "预览缩放",
        LanguageLabel = "语言",

        Shape = "类型",
        Size = "大小",
        Thickness = "粗细",
        CentreGap = "中心间隙",
        DarkOutline = "深色描边",
        Reticle = "准星颜色",
        Opacity = "不透明度",
        Subtension = "总宽度",
        PixelUnit = "像素",
        On = "开",
        Off = "关",
        CustomColour = "自定义颜色",

        OverlayRunning = "准星层运行中",
        OverlayStopped = "准星层已停止",
        StartOverlay = "启动准星层",
        RestartOverlay = "重新启动",
        Stop = "停止",
        Reset = "重置",
        Hint = "更改会即时保存并应用。游戏中：Ctrl+Shift+C 隐藏准星，"
             + "Ctrl+Shift+M 打开此窗口，Ctrl+Shift+Q 关闭准星层。",

        ShapeName = t => t switch
        {
            CrosshairType.Dot => "圆点",
            CrosshairType.Circle => "圆环",
            CrosshairType.CrossDot => "十字 + 圆点",
            CrosshairType.CircleDot => "圆环 + 圆点",
            _ => "十字"
        },
        PresetName = p => p switch
        {
            PresetId.Precision => "精准",
            PresetId.MicroDot => "微点",
            PresetId.Ring => "圆环",
            PresetId.Sniper => "狙击",
            PresetId.Neon => "霓虹",
            _ => "经典"
        },
        PresetDescription = p => p switch
        {
            PresetId.Precision => "细十字带中心点",
            PresetId.MicroDot => "极简单点",
            PresetId.Ring => "圆环带中心点",
            PresetId.Sniper => "长臂、开放中心",
            PresetId.Neon => "高对比洋红",
            _ => "均衡的绿色十字"
        }
    };
}
