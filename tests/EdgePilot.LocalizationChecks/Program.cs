using EdgePilot.Core;
using EdgePilot.UI;

var passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine("PASS " + name);
    passed++;
}

Localization.SetLanguage(Language.Italian);
Check(Localization.T("nav.about") == "Informazioni", "Italian navigation translation");
Check(Localization.T("settings.saveChanges") == "Salva modifiche", "Italian settings translation");
Check(Localization.T("tray.exit") == "Esci", "Italian tray translation");

Localization.SetLanguage(Language.English);
Check(Localization.T("nav.about") == "About", "English navigation translation");
Check(Localization.T("settings.saveChanges") == "Save changes", "English settings translation");
Check(Localization.T("tray.exit") == "Exit", "English tray translation");

Localization.SetLanguage(Language.French);
Check(Localization.T("nav.about") == "À propos", "French navigation translation");
Check(Localization.T("settings.saveChanges") == "Enregistrer", "French settings translation");
Check(Localization.T("tray.exit") == "Quitter", "French tray translation");
Check(Localization.T("tooltip.processors", 8).Contains("8"), "formatted translations preserve arguments");
Check(Localization.T("missing.translation.key") == "missing.translation.key", "unknown key has safe fallback");

var directory = Path.Combine(Path.GetTempPath(), "edgepilot-i18n-checks-" + Guid.NewGuid().ToString("N"));
var path = Path.Combine(directory, "settings.json");
try
{
    var french = new NotchPreferences { Language = Language.French };
    PreferenceStore.Save(path, french);
    Check(PreferenceStore.Load(path).Language == Language.French, "explicit language persists");

    var automatic = french with { Language = null };
    PreferenceStore.Save(path, automatic);
    Check(PreferenceStore.Load(path).Language is null, "automatic language persists");

    try
    {
        PreferenceStore.Save(path, new NotchPreferences { Language = (Language)999 });
        Check(false, "invalid language rejected");
    }
    catch (InvalidDataException)
    {
        Check(PreferenceStore.Load(path).Language is null, "invalid language does not replace last good settings");
    }
}
finally
{
    if (Directory.Exists(directory)) Directory.Delete(directory, true);
}

Console.WriteLine($"{passed} localization checks passed.");
