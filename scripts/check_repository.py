"""Check repository links, interface text and commit attribution without printing private values."""
from pathlib import Path
from urllib.parse import unquote
import json
import re
import subprocess

# Keys are lowercase dotted words, which is enough to find them in the sources.
KEY = r'"([a-z][a-z0-9._]*)"'

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
# Interface text lives in Assets/Locales. Every file must define exactly the keys
# English defines, and the code must only ask for keys that exist: a translator
# should never discover a gap by seeing a blank label at run time.
locales = root / "src" / "EdgePilot" / "Assets" / "Locales"
if not (locales / "en.json").is_file():
    errors.append("Missing src/EdgePilot/Assets/Locales/en.json")
else:
    english = set(json.loads((locales / "en.json").read_text(encoding="utf-8")))
    for path in sorted(locales.glob("*.json")):
        keys = set(json.loads(path.read_text(encoding="utf-8")))
        for key in sorted(english - keys):
            errors.append(f"{path.name} does not translate {key}")
        for key in sorted(keys - english):
            errors.append(f"{path.name} defines {key}, which en.json does not")
    literals = set()
    for source in sorted((root / "src" / "EdgePilot").rglob("*.cs")):
        if any(part in ("bin", "obj") for part in source.parts):
            continue
        literals.update(re.findall(KEY, source.read_text(encoding="utf-8")))
    # Keys assembled at run time from a prefix never appear whole in the sources.
    dynamic = tuple("settings.refresh." + str(ms) for ms in (500, 1000, 2000, 5000))
    families = ("about.", "app.", "cli.", "drive.", "format.", "notch.", "platform.",
                "preferences.", "settings.", "tray.", "ui.")
    for key in sorted(english):
        if key.startswith("_") or key in dynamic or key in literals:
            continue
        errors.append(f"en.json defines {key}, which no source file asks for")
    # File names share the shape of a key without being one.
    names = (".json", ".ico", ".svg", ".exe", ".desktop", ".lnk", ".tmp")
    for literal in sorted(literals):
        if not literal.startswith(families) or literal in dynamic or literal.endswith(names) or literal.endswith("."):
            continue
        if literal not in english:
            errors.append(f"A source file asks for {literal}, which en.json does not define")
for required in ("LICENSE", "README.md", "SECURITY.md", "CONTRIBUTING.md", "THIRD-PARTY-NOTICES.md"):
    if not (root / required).is_file():
        errors.append(f"Missing {required}")
if errors:
    raise SystemExit("\n".join(errors))
print("Relative documentation links, interface text, required documents and branch commit attribution passed.")
