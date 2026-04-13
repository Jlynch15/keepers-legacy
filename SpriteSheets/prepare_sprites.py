"""
prepare_sprites.py
Converts all 7 creature sprite sheet SVGs to 512×512 PNGs and places them
into the correct KeeperLegacy/Assets.xcassets imageset folders.

Requirements:
  - Inkscape installed (Windows/Mac/Linux)

Usage (Windows, from the SpriteSheets/ folder):
  python prepare_sprites.py

  If Inkscape is not on PATH, set INKSCAPE_PATH below to the full exe path,
  e.g.: INKSCAPE_PATH = r"C:\\Program Files\\Inkscape\\bin\\inkscape.exe"

Usage (Mac, if you have Inkscape installed):
  python3 prepare_sprites.py
  or with Homebrew: brew install inkscape  then  python3 prepare_sprites.py
"""

import subprocess
import shutil
import sys
from pathlib import Path

# ── Configuration ──────────────────────────────────────────────────────────────

# Path to Inkscape executable. Leave as None to auto-detect from PATH.
# Windows example: r"C:\\Program Files\\Inkscape\\bin\\inkscape.exe"
INKSCAPE_PATH = None

# Directories (relative to this script)
SHEETS_DIR  = Path(__file__).parent                         # SpriteSheets/
ASSETS_DIR  = SHEETS_DIR.parent / "KeeperLegacy" / "Assets.xcassets"

# Map: SVG filename (in SpriteSheets/) → imageset name (in Assets.xcassets/)
SPRITE_SHEETS = {
    "aquaburst_sheet.svg":   "creature_aquaburst_atlas",
    "crumblebane_sheet.svg": "creature_crumblebane_atlas",
    "wildbloom_sheet.svg":   "creature_wildbloom_atlas",
    "cinderborne_sheet.svg": "creature_cinderborne_atlas",
    "frostveil_sheet.svg":   "creature_frostveil_atlas",
    "sparkburst_sheet.svg":  "creature_sparkburst_atlas",
    "arcane_sheet.svg":      "creature_arcane_atlas",
}

OUTPUT_SIZE = 512  # pixels (square — matches the 512×512 SVG viewBox)

# ── Inkscape detection ──────────────────────────────────────────────────────────

def find_inkscape() -> str:
    if INKSCAPE_PATH:
        return INKSCAPE_PATH
    # Common Windows install locations
    candidates = [
        r"C:\Program Files\Inkscape\bin\inkscape.exe",
        r"C:\Program Files (x86)\Inkscape\bin\inkscape.exe",
    ]
    for c in candidates:
        if Path(c).exists():
            return c
    # Try PATH
    found = shutil.which("inkscape")
    if found:
        return found
    return None

# ── Conversion ──────────────────────────────────────────────────────────────────

def convert(svg_path: Path, png_path: Path, inkscape: str) -> bool:
    """Convert SVG to PNG using Inkscape CLI. Returns True on success."""
    cmd = [
        inkscape,
        "--export-type=png",
        f"--export-filename={png_path}",
        f"--export-width={OUTPUT_SIZE}",
        f"--export-height={OUTPUT_SIZE}",
        str(svg_path),
    ]
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        print(f"  ERROR: {result.stderr.strip()}", file=sys.stderr)
        return False
    return True

# ── Main ────────────────────────────────────────────────────────────────────────

def main():
    inkscape = find_inkscape()
    if not inkscape:
        print("ERROR: Inkscape not found.")
        print("  Install from https://inkscape.org or set INKSCAPE_PATH in this script.")
        sys.exit(1)

    print(f"Using Inkscape: {inkscape}")
    print(f"Output size: {OUTPUT_SIZE}×{OUTPUT_SIZE}px\n")

    success_count = 0
    fail_count    = 0

    for svg_name, imageset_name in SPRITE_SHEETS.items():
        svg_path      = SHEETS_DIR / svg_name
        imageset_dir  = ASSETS_DIR / f"{imageset_name}.imageset"
        png_path      = imageset_dir / f"{imageset_name}.png"

        print(f"{svg_name}")

        if not svg_path.exists():
            print(f"  SKIP: file not found at {svg_path}")
            fail_count += 1
            continue

        if not imageset_dir.exists():
            print(f"  SKIP: imageset folder missing at {imageset_dir}")
            fail_count += 1
            continue

        if convert(svg_path, png_path, inkscape):
            size_kb = png_path.stat().st_size // 1024
            print(f"  OK -> {png_path.name}  ({size_kb} KB)")
            success_count += 1
        else:
            fail_count += 1

    print(f"\nDone — {success_count} converted, {fail_count} failed.")
    if fail_count:
        sys.exit(1)

if __name__ == "__main__":
    main()
