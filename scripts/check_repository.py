"""Check repository links and commit attribution without printing private values."""
from pathlib import Path
from urllib.parse import unquote
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
for required in ("LICENSE", "README.md", "SECURITY.md", "CONTRIBUTING.md", "THIRD-PARTY-NOTICES.md"):
    if not (root / required).is_file():
        errors.append(f"Missing {required}")
if errors:
    raise SystemExit("\n".join(errors))
print("Relative documentation links, required documents and branch commit attribution passed.")
