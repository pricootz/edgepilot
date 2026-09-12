"""Preserve dependency metadata and available upstream license files in packages."""
from pathlib import Path
import json
import os
import shutil
import subprocess
import xml.etree.ElementTree as ET

package_root = Path(os.environ.get("EDGEPILOT_PACKAGE_DIR", "dist/EdgePilot"))
out = package_root / "third-party-licenses"
out.mkdir(parents=True, exist_ok=True)
cache = Path(os.environ.get("NUGET_PACKAGES", str(Path.home() / ".nuget/packages")))
assets = json.loads(Path("src/EdgePilot/obj/project.assets.json").read_text(encoding="utf-8"))
inventory = []
for name, entry in assets["libraries"].items():
    if entry.get("type") != "package":
        continue
    directory = cache / entry.get("path", name.lower())
    item = {"package": name}
    for nuspec in directory.glob("*.nuspec"):
        tree = ET.parse(nuspec)
        for element in tree.iter():
            key = element.tag.split("}")[-1]
            if key in ("license", "licenseUrl", "projectUrl") and element.text:
                item[key] = element.text
        target = out / name.replace("/", "-")
        target.mkdir(parents=True, exist_ok=True)
        shutil.copy2(nuspec, target / nuspec.name)
    if directory.exists():
        for source in directory.rglob("*"):
            if source.is_file() and any(word in source.name.lower() for word in ("license", "licence", "notice", "copying")):
                target = out / name.replace("/", "-") / source.relative_to(directory)
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(source, target)
    inventory.append(item)
# Self-contained runtime packs are resolved outside normal package references on some SDKs.
dotnet = Path(shutil.which("dotnet")).resolve().parent
for base in (cache, dotnet / "packs"):
    for pack in base.glob("*"):
        if pack.is_dir() and "runtime" in pack.name.lower():
            for source in pack.rglob("*"):
                if source.is_file() and any(word in source.name.lower() for word in ("license", "licence", "notice")):
                    target = out / "runtime-packs" / pack.name / source.relative_to(pack)
                    target.parent.mkdir(parents=True, exist_ok=True)
                    shutil.copy2(source, target)
(out / "dependencies.json").write_text(json.dumps(inventory, indent=2) + "\n", encoding="utf-8")
for name in ("LICENSE", "THIRD-PARTY-NOTICES.md"):
    shutil.copy2(name, package_root / name)
repository_licenses = Path("licenses")
if repository_licenses.is_dir():
    shutil.copytree(repository_licenses, package_root / "licenses", dirs_exist_ok=True)
assert inventory, "Dependency inventory is empty"
print(f"Preserved metadata for {len(inventory)} packages and available upstream notices.")
