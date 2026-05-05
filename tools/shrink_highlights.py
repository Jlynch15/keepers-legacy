"""
shrink_highlights.py — one-time fix.

The existing creature SVGs all use a similar "upper-left highlight crescent"
ellipse with rx ~= 70-80 and ry ~= 40-50, applied at roughly the same canvas
coordinates. That makes them look templated/copy-pasted across creatures.

This script walks every <id>.svg under Sprites/Creatures/<biome>/<id>/ and:
- Reduces every body-highlight ellipse rx/ry by 35% (so it fits inside body
  silhouettes and reads as subtle volume rather than a pasted-on oval).
- Drops opacity from 0.55 to 0.42 if explicitly set.

Future creatures should use varied highlight shapes (paths, multi-blob, curved
strokes) per the locked-baseline rules — see the spec.
"""
import re
from pathlib import Path

ROOT      = Path(__file__).resolve().parent.parent
CREATURES = ROOT / "KeeperLegacyGodot" / "Sprites" / "Creatures"

# Match ellipse with class="body-highlight" — capture rx/ry numbers so we can rescale.
ELLIPSE_RE = re.compile(
    r'(<ellipse[^>]*?\bclass="body-highlight"[^>]*?/>)',
    re.DOTALL,
)
RX_RE = re.compile(r'\brx="(\d+)"')
RY_RE = re.compile(r'\bry="(\d+)"')
OPACITY_RE = re.compile(r'\bopacity="0\.55"')


def shrink_one(ellipse: str) -> str:
    def shrink_rx(m):
        return f'rx="{int(int(m.group(1)) * 0.65)}"'

    def shrink_ry(m):
        return f'ry="{int(int(m.group(1)) * 0.65)}"'

    out = RX_RE.sub(shrink_rx, ellipse)
    out = RY_RE.sub(shrink_ry, out)
    out = OPACITY_RE.sub('opacity="0.42"', out)
    return out


def process_svg(path: Path) -> bool:
    text = path.read_text(encoding="utf-8")
    new_text = ELLIPSE_RE.sub(lambda m: shrink_one(m.group(1)), text)
    if new_text != text:
        path.write_text(new_text, encoding="utf-8")
        return True
    return False


def main() -> None:
    changed = 0
    scanned = 0
    for biome_dir in sorted(CREATURES.iterdir()):
        if not biome_dir.is_dir() or biome_dir.name.startswith("_"):
            continue
        for creature_dir in sorted(biome_dir.iterdir()):
            if not creature_dir.is_dir() or creature_dir.name.startswith("_"):
                continue
            svg = creature_dir / f"{creature_dir.name}.svg"
            if not svg.exists():
                continue
            scanned += 1
            if process_svg(svg):
                print(f"  shrunk highlights in {biome_dir.name}/{creature_dir.name}")
                changed += 1
    print(f"\nDone. {changed}/{scanned} SVGs updated.")


if __name__ == "__main__":
    main()
