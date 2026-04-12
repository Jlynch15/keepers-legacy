"""
extract_clean_atlas.py

Reads the Keeper's Legacy SVG sprite sheets (680x830 format with headers/margins)
and exports a clean 512x512 PNG atlas with no headers, no gaps, no colored backgrounds.

Each output atlas is a 4x4 grid of 128x128 transparent-background cells:
  Col 0-3 = mutations,  Row 0-3 = Idle / Interact / Happy / Sad

Requirements (install once):
    pip install cairosvg pillow

Usage:
    python extract_clean_atlas.py
    (run from the SpriteSheets/ folder)
"""

import os
import sys
from pathlib import Path

try:
    import cairosvg
    from PIL import Image
    import io
except ImportError:
    print("Missing libraries. Run:  pip install cairosvg pillow")
    sys.exit(1)

# ── Layout of the source SVG sheets ───────────────────────────────────────────
# Measured from the SVG source (680x830 viewBox).
# Each cell is 128x128.  Cells start at x=82, y=88 with 16px gaps between them.

CELL_W     = 128
CELL_H     = 128
COL_START  = 82      # x of first column's left edge
ROW_START  = 88      # y of first row's top edge
COL_GAP    = 16      # horizontal gap between cells
ROW_GAP    = 30      # vertical gap between rows (measured: row2 starts at y=246)

# Exact cell origins — computed from the constants above
def cell_origin(col: int, row: int) -> tuple[int, int]:
    x = COL_START + col * (CELL_W + COL_GAP)
    y = ROW_START + row * (CELL_H + ROW_GAP)
    return x, y

OUTPUT_W = 512   # 4 * 128
OUTPUT_H = 512   # 4 * 128

# ── Render scale: SVG is 680px wide, we render at 2x for quality then crop ────
RENDER_SCALE = 2.0
SVG_NATURAL_W = 680


def process_sheet(svg_path: Path, output_dir: Path):
    creature_id = svg_path.stem.replace("_sprite_sheet", "")
    print(f"  Processing {creature_id}...")

    # Render entire SVG to PNG at 2x resolution
    svg_bytes  = svg_path.read_bytes()
    png_bytes  = cairosvg.svg2png(
        bytestring=svg_bytes,
        scale=RENDER_SCALE,
        background_color="white"   # needed for cairosvg
    )
    full_img   = Image.open(io.BytesIO(png_bytes)).convert("RGBA")

    # Build clean 512x512 atlas
    atlas = Image.new("RGBA", (OUTPUT_W, OUTPUT_H), (0, 0, 0, 0))

    for row in range(4):
        for col in range(4):
            sx, sy     = cell_origin(col, row)
            # Scale to match the 2x render
            sx2        = int(sx * RENDER_SCALE)
            sy2        = int(sy * RENDER_SCALE)
            cw2        = int(CELL_W * RENDER_SCALE)
            ch2        = int(CELL_H * RENDER_SCALE)
            # Crop cell from full render
            cell_raw   = full_img.crop((sx2, sy2, sx2 + cw2, sy2 + ch2))
            # Resize to 128x128
            cell_sized = cell_raw.resize((CELL_W, CELL_H), Image.LANCZOS)
            # Remove colored background: make near-white pixels transparent
            cell_sized = remove_background(cell_sized)
            # Paste into atlas at clean grid position
            atlas.paste(cell_sized, (col * CELL_W, row * CELL_H), cell_sized)

    out_path = output_dir / f"{creature_id}_sheet.png"
    atlas.save(out_path, "PNG")
    print(f"    → Saved {out_path.name}  ({OUTPUT_W}x{OUTPUT_H})")
    return out_path


def remove_background(img: Image.Image, threshold: int = 230) -> Image.Image:
    """
    Make near-white and near-pastel background pixels transparent.
    Adjust 'threshold' (0-255) if too much or too little is removed.
    Higher = removes more background.  Lower = keeps more.
    """
    img    = img.convert("RGBA")
    data   = img.load()
    width, height = img.size

    for y in range(height):
        for x in range(width):
            r, g, b, a = data[x, y]
            # Remove pixels that are very light (background)
            brightness = (r + g + b) / 3
            if brightness > threshold:
                # Feather the edge slightly
                alpha = max(0, int((255 - brightness) * 3))
                data[x, y] = (r, g, b, alpha)

    return img


def main():
    sheets_dir = Path(__file__).parent
    output_dir = sheets_dir / "clean_atlases"
    output_dir.mkdir(exist_ok=True)

    svgs = sorted(sheets_dir.glob("*_sprite_sheet.svg"))
    if not svgs:
        print("No *_sprite_sheet.svg files found in", sheets_dir)
        sys.exit(1)

    print(f"Found {len(svgs)} sprite sheet(s). Extracting clean atlases...\n")
    for svg in svgs:
        process_sheet(svg, output_dir)

    print(f"\nDone! Clean atlases saved to:  {output_dir}")
    print("\nNext step:")
    print("  1. Review the PNGs in clean_atlases/")
    print("  2. Copy them to  KeeperLegacy/Assets.xcassets/Creatures/")
    print("  3. Name each imageset:  {creatureID}_sheet  (e.g. aquaburst_sheet)")


if __name__ == "__main__":
    main()
