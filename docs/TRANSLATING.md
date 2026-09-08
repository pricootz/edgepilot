# Translating EdgePilot

EdgePilot interface text lives in `src/EdgePilot/Assets/Locales`. Each shipped language is one JSON file, embedded at build time.

## Add a language

1. Copy `src/EdgePilot/Assets/Locales/en.json` to a new file named with the language tag, for example `de.json`, `es.json` or `pt-BR.json`.
2. Set `_language` to the name of the language written in that language, for example `Deutsch`, `Español` or `Português (Brasil)`.
3. Translate the values on the right-hand side. Do not change the keys.
4. Keep placeholders such as `{0}` and format specifiers such as `{0:0.##}` intact.
5. Run the repository and localization checks before opening a pull request.

```bash
python scripts/check_repository.py
dotnet run --project tests/EdgePilot.LocalizationChecks -c Release
dotnet run --project tests/EdgePilot.UxChecks -c Release
```

The project embeds `Assets/Locales/*.json` with a wildcard, so adding a language does not require editing C# or the project file. A new application build is still required because the locale files are embedded in the application assembly.

## Fallback behavior

EdgePilot resolves language requests in this order:

1. exact language tag, for example `pt-BR`;
2. neutral language tag when available, for example `pt`;
3. English;
4. the translation key itself as a final safe fallback.

Automatic mode follows the operating-system UI culture. If a saved language is no longer shipped, EdgePilot falls back safely instead of rejecting the whole settings file.

## Existing preferences

The v0.2 preview originally stored the enum names `Italian`, `English` and `French`. The current preference loader accepts those legacy values and maps them to `it`, `en` and `fr`, so upgrading does not discard existing settings.

## Rules enforced by CI

- `en.json` is the canonical key set.
- Every shipped locale must define the same keys as English.
- Extra or missing keys fail the repository checks.
- Code must not request localization keys that do not exist.
- Locale files must be valid JSON.

## Placeholders and formatting

Placeholders are values inserted by the app:

```json
"tooltip.processors": "{0} logical processors"
```

Their order may change when a language needs it, but do not remove them.

Format specifiers control numeric formatting:

```json
"drive.capacity.tb": "{0:0.##} TB"
```

Keep the format specifier intact. Decimal separators follow the selected locale, so a value can appear as `16.5 TB` in English and `16,5 TB` in Italian or French.

## Short labels

The collapsed notch has limited space. Keep values such as ring captions (`ring.disk`, `ring.network`) short enough to remain readable.

## Credits

The first IT / EN / FR localization foundation was contributed by [@IamArayel](https://github.com/IamArayel) in [PR #5](https://github.com/pricootz/edgepilot/pull/5).

The file-based locale architecture, translation workflow, fallback strategy and repository validation ideas were proposed by [@ArnieGA](https://github.com/ArnieGA) in [PR #8](https://github.com/pricootz/edgepilot/pull/8) and reconciled with the current v0.2 Settings architecture.
