"""Render _fallback.svg -> _fallback.png at 512x512 via Inkscape CLI."""
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SVG = ROOT / "KeeperLegacyGodot/Sprites/Creatures/_fallback.svg"
PNG = ROOT / "KeeperLegacyGodot/Sprites/Creatures/_fallback.png"


def find_inkscape() -> str | None:
    for c in [r"C:\Program Files\Inkscape\bin\inkscape.exe",
              r"C:\Program Files (x86)\Inkscape\bin\inkscape.exe"]:
        if Path(c).exists():
            return c
    return shutil.which("inkscape")


def main():
    ink = find_inkscape()
    if not ink:
        sys.exit("ERROR: Inkscape not found. Install from https://inkscape.org")
    subprocess.run(
        [ink, "--export-type=png", f"--export-filename={PNG}",
         "--export-width=512", "--export-height=512", str(SVG)],
        check=True,
    )
    print(f"OK -> {PNG}")


if __name__ == "__main__":
    main()
