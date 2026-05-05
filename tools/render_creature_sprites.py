"""
render_creature_sprites.py
Walks KeeperLegacyGodot/Sprites/Creatures/*/, for each <id>.svg:
  1. Parses the PALETTE comment (4 mutations, class -> hex color).
  2. Generates 4 temp SVGs with class fills overridden per mutation.
  3. Renders each to <id>_v<N>.png at 512x512 via Inkscape CLI.

Usage:
  python tools/render_creature_sprites.py                  # render all
  python tools/render_creature_sprites.py coralsprite      # render one
  python tools/render_creature_sprites.py coralsprite --pilot-render
                                                           # render into <id>/_svg_pilot/
"""
import argparse
import re
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT          = Path(__file__).resolve().parent.parent
CREATURES_DIR = ROOT / "KeeperLegacyGodot" / "Sprites" / "Creatures"
OUTPUT_SIZE   = 512

# ── Inkscape detection ─────────────────────────────────────────────────────────

def find_inkscape() -> str | None:
    for c in [r"C:\Program Files\Inkscape\bin\inkscape.exe",
              r"C:\Program Files (x86)\Inkscape\bin\inkscape.exe"]:
        if Path(c).exists():
            return c
    return shutil.which("inkscape")

# ── Palette parsing ────────────────────────────────────────────────────────────

PALETTE_BLOCK_RE = re.compile(r"<!--\s*PALETTE\s*(.*?)-->", re.DOTALL)
MUTATION_LINE_RE = re.compile(r"v([1-4])\s*:\s*(.*)")
KV_RE            = re.compile(r"([a-z][a-z0-9-]*)\s*=\s*(#[0-9a-fA-F]{3,6})")


def parse_palette(svg_text: str) -> dict[int, dict[str, str]]:
    """Extract {mutation_index: {class_name: hex_color}} from the PALETTE comment."""
    block = PALETTE_BLOCK_RE.search(svg_text)
    if not block:
        raise ValueError("SVG missing <!-- PALETTE ... --> comment block")
    palettes: dict[int, dict[str, str]] = {}
    for raw in block.group(1).splitlines():
        line = raw.strip()
        if not line:
            continue
        m = MUTATION_LINE_RE.match(line)
        if not m:
            continue
        mutation = int(m.group(1))
        palettes[mutation] = dict(KV_RE.findall(m.group(2)))
    if set(palettes.keys()) != {1, 2, 3, 4}:
        raise ValueError(f"PALETTE must define v1..v4, got {sorted(palettes.keys())}")
    return palettes

# ── Style rewriting ────────────────────────────────────────────────────────────

CLASS_RULE_RE = re.compile(
    r"(\.([a-z][a-z0-9-]*)\s*\{\s*(?:fill|stroke):\s*)#[0-9a-fA-F]{3,6}(\s*;\s*\})"
)


def apply_palette(svg_text: str, palette: dict[str, str]) -> str:
    """Rewrite each `.class { fill|stroke: #...; }` rule in the <style> block with the palette's color."""
    def replace(match: re.Match) -> str:
        prefix, cls, suffix = match.group(1), match.group(2), match.group(3)
        color = palette.get(cls)
        if color is None:
            return match.group(0)  # no override for this class
        return f"{prefix}{color}{suffix}"
    return CLASS_RULE_RE.sub(replace, svg_text)

# ── Rendering ──────────────────────────────────────────────────────────────────

def render_svg_to_png(inkscape: str, svg_path: Path, png_path: Path) -> None:
    cmd = [
        inkscape, "--export-type=png", f"--export-filename={png_path}",
        f"--export-width={OUTPUT_SIZE}", f"--export-height={OUTPUT_SIZE}",
        str(svg_path),
    ]
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        raise RuntimeError(f"Inkscape failed: {result.stderr.strip()}")


def render_creature(inkscape: str, creature_dir: Path, pilot_render: bool = False) -> None:
    creature_id = creature_dir.name
    svg_path    = creature_dir / f"{creature_id}.svg"
    if not svg_path.exists():
        print(f"  SKIP: {svg_path.name} not found")
        return
    out_dir = creature_dir / "_svg_pilot" if pilot_render else creature_dir
    out_dir.mkdir(exist_ok=True)
    svg_text = svg_path.read_text(encoding="utf-8")
    palettes = parse_palette(svg_text)
    for mutation in (1, 2, 3, 4):
        out_svg  = apply_palette(svg_text, palettes[mutation])
        png_path = out_dir / f"{creature_id}_v{mutation}.png"
        with tempfile.NamedTemporaryFile(
            suffix=".svg", delete=False, mode="w", encoding="utf-8"
        ) as tmp:
            tmp.write(out_svg)
            tmp_path = Path(tmp.name)
        try:
            render_svg_to_png(inkscape, tmp_path, png_path)
            rel = png_path.relative_to(ROOT)
            print(f"  OK -> {rel}")
        finally:
            tmp_path.unlink(missing_ok=True)

# ── Main ───────────────────────────────────────────────────────────────────────

def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("creature", nargs="?", help="Creature ID (omit for all)")
    parser.add_argument(
        "--pilot-render", action="store_true",
        help="Render into <id>/_svg_pilot/ instead of overwriting canonical PNGs. "
             "Use for creatures with illustrated migrations (coralsprite, deepecho).",
    )
    args = parser.parse_args()

    inkscape = find_inkscape()
    if not inkscape:
        sys.exit("ERROR: Inkscape not found. Install from https://inkscape.org")

    if args.creature:
        targets = [CREATURES_DIR / args.creature]
    else:
        targets = sorted(
            p for p in CREATURES_DIR.iterdir()
            if p.is_dir() and not p.name.startswith("_")
        )

    for d in targets:
        if not d.is_dir():
            print(f"SKIP: {d} not a directory")
            continue
        print(d.name)
        render_creature(inkscape, d, pilot_render=args.pilot_render)


if __name__ == "__main__":
    main()
