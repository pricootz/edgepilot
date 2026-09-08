using System.Globalization;
using System.Text.Json;

namespace EdgePilot.Core;

public sealed record LanguageOption(Language Value, string Name)
{
    public string Code => Value.Code;
    public override string ToString() => Name;
}

public static class Localization
{
    public const string FallbackCode = "en";
    private const string LocaleMarker = ".Assets.Locales.";
    private const string LocaleSuffix = ".json";
    private const string LanguageNameKey = "_language";

    private static readonly IReadOnlyDictionary<string, Dictionary<string, string>> Catalogs = LoadCatalogs();
    private static Dictionary<string, string> _active = Catalog(FallbackCode);

    internal static Func<Language>? SystemLanguageDetectorOverride { get; set; }

    public static Language Current { get; private set; } = Language.English;

    public static IReadOnlyList<LanguageOption> Available => Catalogs
        .Select(item => new LanguageOption(new Language(item.Key), LanguageName(item.Key)))
        .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
        .ToArray();

    public static string SystemLanguageName => LanguageName(DetectSystemLanguage().Code);

    public static Language DetectSystemLanguage()
    {
        if (SystemLanguageDetectorOverride is not null)
            return Resolve(SystemLanguageDetectorOverride());

        try { return Resolve(new Language(CultureInfo.InstalledUICulture.Name)); }
        catch (ArgumentException) { return new Language(FallbackCode); }
    }

    public static void SetLanguage(Language language)
    {
        Current = Resolve(language);
        _active = Catalog(Current.Code);
        ApplyCulture(Current.Code);
    }

    public static Language NormalizePreference(Language language) => Resolve(language);

    public static string T(string key)
    {
        if (_active.TryGetValue(key, out var value)) return value;
        if (Catalog(FallbackCode).TryGetValue(key, out value)) return value;
        return key;
    }

    public static string T(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, T(key), args);

    public static string In(Language language, string key) => In(language.Code, key);

    public static string In(string code, string key)
    {
        Language requested;
        try { requested = new Language(code); }
        catch (ArgumentException) { requested = Language.English; }
        var resolved = Resolve(requested);
        if (Catalog(resolved.Code).TryGetValue(key, out var value)) return value;
        if (Catalog(FallbackCode).TryGetValue(key, out value)) return value;
        return key;
    }

    public static IReadOnlyCollection<string> Keys(Language language) => Catalog(Resolve(language).Code).Keys;

    public static Language Resolve(Language requested)
    {
        if (Catalogs.ContainsKey(requested.Code)) return CanonicalLanguage(requested.Code);

        var separator = requested.Code.IndexOf('-');
        if (separator > 0)
        {
            var neutral = requested.Code[..separator];
            if (Catalogs.ContainsKey(neutral)) return CanonicalLanguage(neutral);
        }

        return Catalogs.ContainsKey(FallbackCode)
            ? new Language(FallbackCode)
            : new Language(Catalogs.Keys.First());
    }

    private static Language CanonicalLanguage(string requested) =>
        new(Catalogs.Keys.First(key => key.Equals(requested, StringComparison.OrdinalIgnoreCase)));

    private static string LanguageName(string code) =>
        Catalog(code).TryGetValue(LanguageNameKey, out var name) && !string.IsNullOrWhiteSpace(name)
            ? name
            : code;

    private static Dictionary<string, string> Catalog(string code) =>
        Catalogs.TryGetValue(code, out var catalog)
            ? catalog
            : Catalogs.TryGetValue(FallbackCode, out var fallback)
                ? fallback
                : new Dictionary<string, string>();

    private static void ApplyCulture(string code)
    {
        CultureInfo culture;
        try { culture = CultureInfo.GetCultureInfo(code); }
        catch (CultureNotFoundException) { culture = CultureInfo.InvariantCulture; }

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    private static IReadOnlyDictionary<string, Dictionary<string, string>> LoadCatalogs()
    {
        var assembly = typeof(Localization).Assembly;
        var catalogs = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var resource in assembly.GetManifestResourceNames())
        {
            var marker = resource.IndexOf(LocaleMarker, StringComparison.Ordinal);
            if (marker < 0 || !resource.EndsWith(LocaleSuffix, StringComparison.OrdinalIgnoreCase)) continue;

            var codeStart = marker + LocaleMarker.Length;
            var code = resource[codeStart..^LocaleSuffix.Length];
            if (string.IsNullOrWhiteSpace(code)) continue;

            try
            {
                using var stream = assembly.GetManifestResourceStream(resource);
                if (stream is null) continue;
                var catalog = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
                if (catalog is { Count: > 0 }) catalogs[code] = catalog;
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Trace.WriteLine($"Skipping malformed locale {resource}: {ex.Message}");
            }
        }

        if (!catalogs.ContainsKey(FallbackCode))
            throw new InvalidOperationException("The embedded English localization catalog is missing.");

        return catalogs;
    }
}