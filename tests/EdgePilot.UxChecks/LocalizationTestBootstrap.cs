using System.Runtime.CompilerServices;
using EdgePilot.Core;

internal static class LocalizationTestBootstrap
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        Localization.SystemLanguageDetectorOverride = () => Language.Italian;
        Localization.SetLanguage(Language.Italian);
    }
}
