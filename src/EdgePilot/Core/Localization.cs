using System.Globalization;

namespace EdgePilot.Core;

public static class Localization
{
    public static Language Current { get; private set; } = Language.English;

    internal static Func<Language>? SystemLanguageDetectorOverride { get; set; }

    public static Language DetectSystemLanguage()
    {
        if (SystemLanguageDetectorOverride is not null) return SystemLanguageDetectorOverride();
        return CultureInfo.InstalledUICulture.TwoLetterISOLanguageName switch
        {
            "it" => Language.Italian,
            "fr" => Language.French,
            _ => Language.English
        };
    }

    public static void SetLanguage(Language language)
    {
        Current = language;
        var culture = CultureInfo.GetCultureInfo(language switch
        {
            Language.Italian => "it-IT",
            Language.French => "fr-FR",
            _ => "en-US"
        });
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public static string T(string key) => Strings.Catalog.TryGetValue(key, out var translations)
        ? translations[Current]
        : key;

    public static string T(string key, params object?[] args) => string.Format(T(key), args);
}
