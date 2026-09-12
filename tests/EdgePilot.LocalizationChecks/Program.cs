using System.Globalization;
using EdgePilot.Core;
using EdgePilot.UI;

var passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine("PASS " + name);
    passed++;
}

var available = Localization.Available;
Check(available.Any(x => x.Value == Language.Italian && x.Name == "Italiano"), "Italian locale discovered from assets");
Check(available.Any(x => x.Value == Language.English && x.Name == "English"), "English locale discovered from assets");
Check(available.Any(x => x.Value == Language.French && x.Name == "Français"), "French locale discovered from assets");

var englishKeys = Localization.Keys(Language.English).ToHashSet(StringComparer.Ordinal);
foreach (var locale in available)
    Check(englishKeys.SetEquals(Localization.Keys(locale.Value)), $"locale key parity {locale.Code}");

Localization.SetLanguage(Language.Italian);
Check(Localization.T("nav.about") == "Informazioni", "Italian navigation translation");
Check(Localization.T("settings.saveChanges") == "Salva modifiche", "Italian settings translation");
Check(Localization.T("tray.exit") == "Esci", "Italian tray translation");
Check(Localization.T("tray.moveHere") == "Sposta EdgePilot su questo monitor",
    "Italian display recovery translation");
Check(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == ",", "Italian number culture");

Localization.SetLanguage(Language.English);
Check(Localization.T("nav.about") == "About", "English navigation translation");
Check(Localization.T("settings.saveChanges") == "Save changes", "English settings translation");
Check(Localization.T("tray.exit") == "Exit", "English tray translation");
Check(Localization.T("display.useCurrent") == "Use the display containing Settings",
    "English current-display action translation");
Check(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == ".", "English number culture");

Localization.SetLanguage(Language.French);
Check(Localization.T("nav.about") == "À propos", "French navigation translation");
Check(Localization.T("settings.saveChanges") == "Enregistrer", "French settings translation");
Check(Localization.T("tray.exit") == "Quitter", "French tray translation");
Check(Localization.T("tooltip.processors", 8).Contains("8"), "formatted translations preserve arguments");
Check(Localization.T("missing.translation.key") == "missing.translation.key", "unknown key has safe fallback");

Check(Localization.Resolve(new Language("fr-FR")) == Language.French, "regional locale resolves to shipped neutral locale");
Check(Localization.Resolve(new Language("de-DE")) == Language.English, "unshipped locale falls back to English");
Check(Localization.In(new Language("de"), "nav.about") == "About", "named unshipped locale uses English fallback");

var directory = Path.Combine(Path.GetTempPath(), "edgepilot-i18n-checks-" + Guid.NewGuid().ToString("N"));
var path = Path.Combine(directory, "settings.json");
try
{
    var french = new NotchPreferences
    {
        Language = Language.French,
        SettingsTheme = SettingsThemePreference.Dark
    };
    PreferenceStore.Save(path, french);
    var loadedFrench = PreferenceStore.Load(path);
    Check(loadedFrench.Language == Language.French, "explicit language persists");
    Check(loadedFrench.SettingsTheme == SettingsThemePreference.Dark, "settings theme persists");

    var automatic = french with { Language = null, SettingsTheme = SettingsThemePreference.System };
    PreferenceStore.Save(path, automatic);
    Check(PreferenceStore.Load(path).Language is null, "automatic language persists");

    PreferenceStore.Save(path, french);
    var legacyJson = File.ReadAllText(path).Replace("\"fr\"", "\"French\"");
    File.WriteAllText(path, legacyJson);
    Check(PreferenceStore.Load(path).Language == Language.French, "legacy enum language migrates to locale code");

    PreferenceStore.Save(path, french);
    var missingLocaleJson = File.ReadAllText(path).Replace("\"fr\"", "\"de\"");
    File.WriteAllText(path, missingLocaleJson);
    Check(PreferenceStore.Load(path).Language == Language.English, "removed locale falls back without invalidating settings");

    try
    {
        _ = new Language("not a locale");
        Check(false, "invalid language code rejected");
    }
    catch (ArgumentException)
    {
        Check(true, "invalid language code rejected");
    }
}
finally
{
    if (Directory.Exists(directory)) Directory.Delete(directory, true);
}

Console.WriteLine($"{passed} localization checks passed.");
