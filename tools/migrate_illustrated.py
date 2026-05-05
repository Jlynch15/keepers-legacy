"""
migrate_illustrated.py
Copies the 3 illustrated PNGs from CharacterModels/ into the canonical
Sprites/Creatures/<biome>/<id>/ structure with backgrounds removed and 1-indexed names.

Mutation order matches CreatureRosterData.cs.
"""
import io
from pathlib import Path
from PIL import Image
from rembg import remove

ROOT = Path(__file__).resolve().parent.parent
SRC  = ROOT / "CharacterModels"
DST  = ROOT / "KeeperLegacyGodot" / "Sprites" / "Creatures"

# id -> (biome, ordered list of source filenames matching mutation index 0..3)
MAPPING = {
    "coralsprite": ("water", ["VividOrange.png", "PinkCoral.png", "PaleYellow.png", "Purple.png"]),
    "deepecho":    ("water", ["AbyssBlack.png",  "BioluminescentBlue.png", "DeepPurple.png", "Emerald.png"]),
    "tidecaller":  ("water", ["StormGrey.png",   "Seafoam.png",  "SandyTan.png",   "SlateBlue.png"]),
}


def main() -> None:
    for creature_id, (biome, source_names) in MAPPING.items():
        out_dir = DST / biome / creature_id
        out_dir.mkdir(parents=True, exist_ok=True)
        # Source dir uses CamelCase folder name (Coralsprite, Deepecho, Tidecaller)
        src_dir = SRC / creature_id.capitalize()
        for idx, name in enumerate(source_names):
            src_file = src_dir / name
            dst_file = out_dir / f"{creature_id}_v{idx+1}.png"
            if not src_file.exists():
                print(f"  MISS: {src_file} not found, skipping {dst_file.name}")
                continue
            print(f"  {creature_id}/{name} -> {dst_file.relative_to(ROOT)}")
            with src_file.open("rb") as f:
                cut = remove(f.read())
            img = Image.open(io.BytesIO(cut)).convert("RGBA")
            img.save(dst_file)


if __name__ == "__main__":
    main()
