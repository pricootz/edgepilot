"""Check repository links, locale catalogs and private commit attribution."""
from pathlib import Path
from urllib.parse import unquote
import json
import re
import subprocess

root = Path.cwd().resolve()
errors = []

for doc in root.rglob("*.md"):
    if any(part in (".git", "bin", "obj", "dist") for part in doc.parts):
        continue
    text = doc.read_text(encoding="utf-8")
    links = re.findall(r'\]\(([^)]+)\)|src="([^"]+)"', text)
    for groups in links:
        link = next(x for x in groups if x)
        if link.startswith(("https:", "http:", "mailto:", "#", "data:")):
            continue
        target = (doc.parent / unquote(link.split("#")[0])).resolve()
        if not target.exists():
            errors.append(f"Missing link target in {doc.relative_to(root)}: {link}")

# Scan published branch history; GitHub's legacy pull refs and caches require a separate check.
emails = subprocess.check_output(
    ["git", "log", "HEAD", "--format=%ae%n%ce"], text=True
).splitlines()
if any(not (email.endswith("@users.noreply.github.com") or email == "noreply@github.com") for email in emails):
    errors.append("A commit in this branch history uses a non-private email address.")

# Translation files are intentionally contributor-friendly: every shipped locale must
# match the English catalog key-for-key, and every application key must be used by source.
locales_dir = root / "src" / "EdgePilot" / "Assets" / "Locales"
english_path = locales_dir / "en.json"
locale_catalogs = {}
if not english_path.is_file():
    errors.append("Missing src/EdgePilot/Assets/Locales/en.json")
else:
    for path in sorted(locales_dir.glob("*.json")):
        try:
            catalog = json.loads(path.read_text(encoding="utf-8"))
        except (json.JSONDecodeError, UnicodeDecodeError) as exc:
            errors.append(f"Invalid locale JSON in {path.relative_to(root)}: {exc}")
            continue
        if not isinstance(catalog, dict) or not catalog:
            errors.append(f"Locale catalog is empty or invalid: {path.relative_to(root)}")
            continue
        if not isinstance(catalog.get("_language"), str) or not catalog["_language"].strip():
            errors.append(f"{path.name} must define a non-empty _language name")
        locale_catalogs[path.name] = catalog

if "en.json" in locale_catalogs:
    english_keys = set(locale_catalogs["en.json"])
    for name, catalog in sorted(locale_catalogs.items()):
        keys = set(catalog)
        for key in sorted(english_keys - keys):
            errors.append(f"{name} does not translate {key}")
        for key in sorted(keys - english_keys):
            errors.append(f"{name} defines {key}, which en.json does not")

    used_keys = set()
    source_root = root / "src" / "EdgePilot"
    patterns = (
        re.compile(r'Localization\.T\(\s*"([a-z][a-z0-9._]*)"'),
        re.compile(r'Localization\.In\([^,\n]+,\s*"([a-z][a-z0-9._]*)"'),
    )
    for source in sorted(source_root.rglob("*.cs")):
        if any(part in ("bin", "obj") for part in source.parts):
            continue
        text = source.read_text(encoding="utf-8")
        for pattern in patterns:
            used_keys.update(pattern.findall(text))

    defined_keys = english_keys - {"_language"}
    for key in sorted(used_keys - defined_keys):
        errors.append(f"A source file asks for {key}, which en.json does not define")
    for key in sorted(defined_keys - used_keys):
        errors.append(f"en.json defines {key}, which no source file asks for")

for required in ("LICENSE", "README.md", "SECURITY.md", "CONTRIBUTING.md", "THIRD-PARTY-NOTICES.md"):
    if not (root / required).is_file():
        errors.append(f"Missing {required}")

if errors:
    raise SystemExit("\n".join(errors))
print("Documentation links, locale catalogs, required documents and private commit attribution passed.")