"""Convert the approved raster master to application formats; run on GitHub CI."""
from pathlib import Path
import base64
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[1]
master = Image.open(root / "docs/assets/edgepilot-master.png").convert("RGBA")
alpha = master.getchannel("A")
assert alpha.getextrema()[0] == 0, "The master must have a transparent background"
assert alpha.getextrema()[1] == 255, "The master must contain opaque artwork"
assert master.width == master.height, "The master must be square"
print(f"Master: {master.size}, alpha={alpha.getextrema()}, content bounds={alpha.getbbox()}")

assets = root / "src/EdgePilot/Assets"
assets.mkdir(parents=True, exist_ok=True)
icon = master.resize((256, 256), Image.Resampling.LANCZOS)
sizes = [(n, n) for n in (16, 24, 32, 48, 64, 128, 256)]
icon.save(assets / "edgepilot.ico", sizes=sizes)
master.resize((512, 512), Image.Resampling.LANCZOS).save(assets / "edgepilot.png")
with Image.open(assets / "edgepilot.ico") as check:
    assert check.ico.sizes() == set(sizes), "Missing ICO resolution"
    for size in sizes:
        assert check.ico.getimage(size).getchannel("A").getextrema()[0] == 0

# Actual-size previews on light and dark surfaces for reviewing small-icon legibility.
sheet = Image.new("RGB", (560, 230), "#f2f2f2")
draw = ImageDraw.Draw(sheet)
draw.rectangle((0, 115, 560, 230), fill="#202020")
for row, color in ((0, "#202020"), (115, "#ffffff")):
    x = 12
    for n in (16, 24, 32, 48, 64):
        draw.text((x, row + 8), str(n), fill=color)
        small = master.resize((n, n), Image.Resampling.LANCZOS)
        sheet.paste(small, (x, row + 35), small)
        x += 100
sheet.save(root / "docs/assets/icon-size-check.png")
print("Generated PNG, seven-size ICO, and actual-size preview.")
