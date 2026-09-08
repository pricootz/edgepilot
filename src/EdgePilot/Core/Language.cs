using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace EdgePilot.Core;

[JsonConverter(typeof(LanguageJsonConverter))]
public readonly record struct Language
{
    private static readonly Regex LanguageTag = new(
        "^[A-Za-z]{2,8}(-[A-Za-z0-9]{2,8})*$",
        RegexOptions.CultureInvariant);

    public static Language Italian => new("it");
    public static Language English => new("en");
    public static Language French => new("fr");

    public string Code { get; }

    public Language(string code)
    {
        var normalized = NormalizeLegacy(code?.Trim() ?? string.Empty);
        if (!LanguageTag.IsMatch(normalized))
            throw new ArgumentException("Invalid language code.", nameof(code));
        Code = normalized.ToLowerInvariant();
    }

    public override string ToString() => Code;

    private static string NormalizeLegacy(string code) => code switch
    {
        var value when value.Equals("Italian", StringComparison.OrdinalIgnoreCase) => "it",
        var value when value.Equals("English", StringComparison.OrdinalIgnoreCase) => "en",
        var value when value.Equals("French", StringComparison.OrdinalIgnoreCase) => "fr",
        _ => code
    };
}

public sealed class LanguageJsonConverter : JsonConverter<Language>
{
    public override Language Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Language must be stored as a locale code string.");
        try { return new Language(reader.GetString() ?? string.Empty); }
        catch (ArgumentException ex) { throw new JsonException("Invalid language code.", ex); }
    }

    public override void Write(Utf8JsonWriter writer, Language value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Code);
}