# Translating EdgePilot

Adding a language is adding one file. No C#, no project edit, no build system knowledge.

## Add a language

1. Copy `src/EdgePilot/Assets/Locales/en.json` to a new file in the same folder, named after the language code: `de.json` for German, `pt-BR.json` for Brazilian Portuguese.
2. Set `_language` to the name of the language **written in that language** — `Deutsch`, not `German`. It is what the settings dropdown shows.
3. Translate the right-hand side of every other line. Leave the keys on the left alone.
4. Build and run. The new language appears in the settings without any further change.

```bash
dotnet run --project tests/EdgePilot.UxChecks -c Release
dotnet run --project src/EdgePilot/EdgePilot.csproj -c Release -- --settings
```

The project embeds `Assets/Locales/*.json` with a wildcard, so the file is picked up on its own.

## Rules the checks enforce

- **Every file defines exactly the keys `en.json` defines.** A missing key fails the UX suite and `scripts/check_repository.py`. English is the fallback at run time, so a gap would show English text in the middle of another language rather than a blank label.
- **Keys are never invented in a language file.** A key the code does not use is dead weight for the next translator.
- Both checks run in CI. Run them locally before opening a pull request.

## Placeholders

`{0}`, `{1}` are values the application fills in. Keep them, and keep their meaning.

```json
"notch.memory.used": "{0} used"
```

Their order may change if the language needs it — `"{1} of {0}"` is fine — as long as every placeholder present in the English line is still present.

Some carry a format after a colon, which controls how the number is printed:

```json
"drive.capacity.tb": "{0:0.##} TB"
"format.percent": "{0:0}%"
```

Keep the part after the colon exactly as it is. The decimal separator follows the language automatically, so Italian shows `16,5 TB` from the same `0.##`.

## Units and short labels

- `format.bytes.units` is a comma-separated list, largest last: `B,KB,MB,GB,TB,PB`. Translate the symbols only if your language really uses different ones.
- `notch.metric.*` are the captions under the rings in the collapsed panel. **Keep them to about five characters** — the cell is 62 pixels wide and longer words are clipped.
- `notch.*.title` and the connection states are drawn in capitals. Write them in capitals.

## What is not translated

Host names, network interface names, disk labels and file paths come from the operating system and are shown as they are.

## Which language a user gets

On first run EdgePilot follows the operating system: an Italian desktop starts in Italian, a Norwegian one starts in English until `no.json` exists. The setting overrides this, and the choice applies immediately — no restart.

If a language file is removed from a later build, anyone who had chosen it falls back to English instead of being locked out of the settings.
