"""One-shot: strip the templated WARM/INTENSE/COSMIC HALO blocks from fire SVGs.
Each fire creature will get a custom glow shape added manually afterward."""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
FIRE = ROOT / "KeeperLegacyGodot" / "Sprites" / "Creatures" / "fire"

# Match the halo comment + following 1-3 concentric circles centered on (256, 296)
HALO_BLOCK = re.compile(
    r"\s*<!--\s*(?:WARM|INTENSE|COSMIC|soft warm).*?HALO.*?-->\s*"
    r"(?:\s*<circle cx=\"256\" cy=\"296\" r=\"\d+\" class=\"accent-base\" opacity=\"0\.\d+\"/>)+",
    re.IGNORECASE,
)

count = 0
for creature_dir in sorted(FIRE.iterdir()):
    svg = creature_dir / f"{creature_dir.name}.svg"
    if not svg.exists():
        continue
    text = svg.read_text(encoding="utf-8")
    new_text = HALO_BLOCK.sub("", text)
    if new_text != text:
        svg.write_text(new_text, encoding="utf-8")
        count += 1
        print(f"  stripped halo from fire/{creature_dir.name}")

print(f"\nDone. {count} files stripped.")
