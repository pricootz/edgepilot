using System.Globalization;
using System.Text.Json;

namespace EdgePilot.Localization;

/// <summary>A shipped language: its file code and the name it gives itself.</summary>
public sealed record LanguageOption(string Code, string Name);

/// <summary>
/// Interface text, one embedded JSON file per language in Assets/Locales.
/// The project embeds that folder with a wildcard, so a new language is a new
/// file and nothing else: this class lists whatever the assembly carries.
/// A key missing from the active language falls back to English, then to the
/// key itself, so a partial translation degrades instead of blanking the UI.
/// </summary>
public static class Strings
{
    public const string FallbackCode = "en";
    private const string ResourcePrefix = "EdgePilot.Assets.Locales.";
    private const string ResourceSuffix = ".json";
    private const string NameKey = "_language";

    private static readonly Dictionary<string, Dictionary<string, string>> Catalogs = LoadAll();
    private static readonly CultureInfo SystemCulture = CultureInfo.CurrentUICulture;
    private static Dictionary<string, string> _active = Catalog(FallbackCode);

    /// <summary>The language actually in use, after resolving the request.</summary>
    public static string ActiveCode { get; private set; } = FallbackCode;

    /// <summary>What was asked for; null means follow the operating system.</summary>
    public static string? Requested { get; private set; }

    /// <summary>Raised after the active language changes, for live relabelling.</summary>
    public static event Action? Changed;

    /// <summary>The language the operating system resolves to, in its own words.</summary>
    public static string SystemLanguageName => Name(Resolve(null));

    public static IReadOnlyList<LanguageOption> Available => Catalogs.Keys
        .Select(code => new LanguageOption(code, Name(code)))
        .OrderBy(option => option.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();

    /// <summary>The keys a language file defines. Used by the key-parity check.</summary>
    public static IReadOnlyCollection<string> Keys(string code) =>
        Catalogs.TryGetValue(code, out var catalog) ? catalog.Keys : Array.Empty<string>();

    /// <summary>
    /// Switches language. An empty request follows the operating system, and an
    /// unknown one falls back rather than throwing: removing a language file
    /// must never lock a user out of the settings that would fix it.
    /// </summary>
    public static void Use(string? requested)
    {
        Requested = string.IsNullOrWhiteSpace(requested) ? null : requested.Trim();
        ActiveCode = Resolve(Requested);
        _active = Catalog(ActiveCode);
        ApplyCulture(ActiveCode);
        Changed?.Invoke();
    }

    public static string Get(string key)
    {
        if (_active.TryGetValue(key, out var value)) return value;
        if (Catalog(FallbackCode).TryGetValue(key, out value)) return value;
        return key;
    }

    /// <summary>Reads a key from a named language, whatever the active one is.</summary>
    public static string In(string code, string key) =>
        Catalog(code).TryGetValue(key, out var value) ? value : Get(key);

    public static string Get(string key, params object?[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), arguments);

    public static string Resolve(string? requested)
    {
        foreach (var candidate in Candidates(requested))
            if (candidate.Length > 0 && Catalogs.ContainsKey(candidate)) return candidate;
        return Catalogs.ContainsKey(FallbackCode)
            ? FallbackCode
            : Catalogs.Keys.FirstOrDefault() ?? FallbackCode;
    }

    private static IEnumerable<string> Candidates(string? requested)
    {
        var wanted = string.IsNullOrWhiteSpace(requested)
            ? SystemCulture.Name
            : requested.Trim();
        yield return wanted;
        var separator = wanted.IndexOf('-');
        if (separator > 0) yield return wanted[..separator];
    }

    private static string Name(string code) =>
        Catalogs.TryGetValue(code, out var catalog) && catalog.TryGetValue(NameKey, out var name)
            ? name
            : code;

    private static Dictionary<string, string> Catalog(string code) =>
        Catalogs.TryGetValue(code, out var catalog) ? catalog : new Dictionary<string, string>();

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

    private static Dictionary<string, Dictionary<string, string>> LoadAll()
    {
        var assembly = typeof(Strings).Assembly;
        var catalogs = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var resource in assembly.GetManifestResourceNames())
        {
            if (!resource.StartsWith(ResourcePrefix, StringComparison.Ordinal) ||
                !resource.EndsWith(ResourceSuffix, StringComparison.OrdinalIgnoreCase))
                continue;
            var code = resource[ResourcePrefix.Length..^ResourceSuffix.Length];
            if (code.Length == 0) continue;
            try
            {
                using var stream = assembly.GetManifestResourceStream(resource);
                if (stream is null) continue;
                var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
                if (parsed is { Count: > 0 }) catalogs[code] = parsed;
            }
            catch (JsonException ex)
            {
                // A malformed contributed file is skipped, never fatal.
                System.Diagnostics.Trace.WriteLine(ex);
            }
        }
        return catalogs;
    }
}
