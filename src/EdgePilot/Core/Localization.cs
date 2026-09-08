using System.Globalization;
using System.Text.Json;

namespace EdgePilot.Core;

public sealed record LanguageOption(string Code, string Name)
{
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

    internal static Func<string>? SystemLanguageDetectorOverride { get; set; }

    public static string Current { get; private set; } = FallbackCode;
    public static string? Requested { get; private set; }

    public static IReadOnlyList<LanguageOption> Available => Catalogs
        .Select(item => new LanguageOption(item.Key, LanguageName(item.Key)))
        .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
        .ToArray();

    public static string SystemLanguageName => LanguageName(DetectSystemLanguage());

    public static string DetectSystemLanguage()
    {
        var requested = SystemLanguageDetectorOverride?.Invoke() ?? CultureInfo.InstalledUICulture.Name;
        return Resolve(requested);
    }

    public static void SetLanguage(string? requested)
    {
        Requested = string.IsNullOrWhiteSpace(requested) ? null : NormalizeLegacyCode(requested.Trim());
        Current = Requested is null ? DetectSystemLanguage() : Resolve(Requested);
        _active = Catalog(Current);
        ApplyCulture(Current);
    }

    public static string T(string key)
    {
        if (_active.TryGetValue(key, out var value)) return value;
        if (Catalog(FallbackCode).TryGetValue(key, out value)) return value;
        return key;
    }

    public static string T(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, T(key), args);

    public static string In(string code, string key)
    {
        var resolved = Resolve(code);
        if (Catalog(resolved).TryGetValue(key, out var value)) return value;
        if (Catalog(FallbackCode).TryGetValue(key, out value)) return value;
        return key;
    }

    public static IReadOnlyCollection<string> Keys(string code) => Catalog(Resolve(code)).Keys;

    public static string Resolve(string? requested)
    {
        var wanted = NormalizeLegacyCode(requested ?? string.Empty);
        if (wanted.Length == 0) wanted = CultureInfo.InstalledUICulture.Name;

        if (Catalogs.ContainsKey(wanted)) return CanonicalCode(wanted);

        var separator = wanted.IndexOf('-');
        if (separator > 0)
        {
            var neutral = wanted[..separator];
            if (Catalogs.ContainsKey(neutral)) return CanonicalCode(neutral);
        }

        return Catalogs.ContainsKey(FallbackCode)
            ? FallbackCode
            : Catalogs.Keys.FirstOrDefault() ?? FallbackCode;
    }

    public static string NormalizePreference(string requested) => Resolve(NormalizeLegacyCode(requested));

    private static string NormalizeLegacyCode(string value) => value switch
    {
        var x when x.Equals("Italian", StringComparison.OrdinalIgnoreCase) => "it",
        var x when x.Equals("English", StringComparison.OrdinalIgnoreCase) => "en",
        var x when x.Equals("French", StringComparison.OrdinalIgnoreCase) => "fr",
        _ => value
    };

    private static string CanonicalCode(string requested) =>
        Catalogs.Keys.First(key => key.Equals(requested, StringComparison.OrdinalIgnoreCase));

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