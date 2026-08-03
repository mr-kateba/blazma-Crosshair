namespace Blazma.Localization;

public enum Language
{
    English,
    Arabic,
    Chinese,
    Russian
}

public static class LanguageCodes
{
    public static string ToCode(Language language) => language switch
    {
        Language.Arabic => "ar",
        Language.Chinese => "zh",
        Language.Russian => "ru",
        _ => "en"
    };

    public static Language FromCode(string code, Language fallback) => code.Trim().ToLowerInvariant() switch
    {
        "en" => Language.English,
        "ar" => Language.Arabic,
        "zh" => Language.Chinese,
        "ru" => Language.Russian,
        _ => fallback
    };

    /// <summary>Label for the language chips. Each is written in its own script.</summary>
    public static string ToChip(Language language) => language switch
    {
        Language.Arabic => "عربي",
        Language.Chinese => "中文",
        Language.Russian => "РУ",
        _ => "EN"
    };
}
