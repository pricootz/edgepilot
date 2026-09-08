"""Generate native icon formats and review previews from the canonical SVG mark."""
from io import BytesIO
from pathlib import Path
import xml.etree.ElementTree as ET

import cairosvg
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[1]
svg_path = root / "src/EdgePilot/Assets/edgepilot.svg"
svg_bytes = svg_path.read_bytes()
svg_root = ET.fromstring(svg_bytes)
assert svg_root.tag.endswith("svg"), "Brand source must be an SVG document"
view_box = svg_root.attrib.get("viewBox", "").split()
assert len(view_box) == 4, "SVG must define a four-value viewBox"
assert float(view_box[2]) == float(view_box[3]), "SVG viewBox must be square"

# Rasterize only for native formats that cannot consume SVG directly.
png_bytes = cairosvg.svg2png(bytestring=svg_bytes, output_width=1024, output_height=1024)
master = Image.open(BytesIO(png_bytes)).convert("RGBA")
alpha = master.getchannel("A")
assert alpha.getextrema()[0] == 0, "The SVG must have a transparent background"
assert alpha.getextrema()[1] == 255, "The SVG must contain opaque artwork"
print(f"Vector master: viewBox={' '.join(view_box)}, alpha={alpha.getextrema()}, content bounds={alpha.getbbox()}")

assets = root / "src/EdgePilot/Assets"
sizes = [(n, n) for n in (16, 24, 32, 48, 64, 128, 256)]
icon = master.resize((256, 256), Image.Resampling.LANCZOS)
icon.save(assets / "edgepilot.ico", sizes=sizes)
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
print("Generated seven-size ICO and actual-size preview from SVG.")
