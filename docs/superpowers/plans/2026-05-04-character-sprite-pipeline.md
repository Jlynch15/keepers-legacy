# Character Sprite Pipeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Spec:** [docs/superpowers/specs/2026-05-04-character-sprite-pipeline-design.md](../specs/2026-05-04-character-sprite-pipeline-design.md)

**Goal:** Establish the placeholder sprite pipeline for all 80 creatures — loader, build script, fallback texture, three pilot creatures, then habitat-by-habitat sweep — with a hard human-review gate after the pilot.

**Architecture:** Add new files only — zero edits to anything that already exists in `KeeperLegacyGodot/`, `JSON Design/`, or `SpriteSheets/`. New files: one C# loader (`Data/CreatureSpriteLoader.cs`), one NUnit test file, one Python build script, one fallback PNG, and one SVG + four PNGs per creature under `Sprites/Creatures/<id>/`. Mutation color swaps are handled by a class-based `<style>` block in each SVG plus a palette comment that the Python script parses and injects per render.

**Tech Stack:** C# / .NET 8 / Godot.NET.Sdk 4.6, NUnit 4 (existing test framework), Python 3 + Inkscape CLI for SVG→PNG rendering, hand-coded SVG.

---

## Critical constraint — protect the parallel session

A separate session is actively working on scene/UI development in `KeeperLegacyGodot/UI/`, scene files (`.tscn`), `project.godot`, and the manager classes. **Every commit in this plan must be verified with `git status` showing only added (`A`) files — never modified (`M`) files** in `KeeperLegacyGodot/`, `JSON Design/`, or `SpriteSheets/`.

If a task ever requires modifying an existing file to proceed, **stop and ask the user** rather than editing it. Adding new files is always safe; modifying anything is not.

## File structure

New files this plan creates:

```
KeeperLegacyGodot/
├── Data/
│   └── CreatureSpriteLoader.cs                    (new)
├── Tests/
│   └── CreatureSpriteLoaderTests.cs               (new)
└── Sprites/
    └── Creatures/
        ├── _fallback.png                          (new — 512×512 gray "?")
        ├── coralsprite/
        │   ├── coralsprite.svg                    (new)
        │   ├── coralsprite_v1.png                 (new — from CharacterModels migration OR rendered)
        │   ├── coralsprite_v2.png                 (new)
        │   ├── coralsprite_v3.png                 (new)
        │   └── coralsprite_v4.png                 (new)
        ├── deepecho/   (same shape)
        ├── tidecaller/ (same shape — migration only, no SVG needed since illustrated PNGs cover it)
        ├── bedrock/    (same shape)
        └── … 76 more creature folders

tools/
└── render_creature_sprites.py                     (new)
```

## SVG authoring conventions (locked in pilot, applied to all 80)

Every creature SVG follows this structure so the build script can render mutation variants:

```svg
<svg viewBox="0 0 512 512" width="512" height="512" xmlns="http://www.w3.org/2000/svg">
  <!-- PALETTE
       v1: body-base=#f06820 body-shadow=#903000 body-highlight=#ff9040 accent-base=#ff5020 …
       v2: body-base=#ff80a0 body-shadow=#a04060 body-highlight=#ffb0c0 accent-base=#e04060 …
       v3: body-base=…
       v4: body-base=…
  -->
  <style>
    .body-base      { fill: #f06820; }   /* defaults match v1 */
    .body-shadow    { fill: #903000; }
    .body-highlight { fill: #ff9040; }
    .accent-base    { fill: #ff5020; }
    /* … one rule per named class … */
  </style>

  <!-- creature artwork using class="body-base" etc. -->
</svg>
```

The build script:
1. Parses the `PALETTE` comment to extract per-mutation class→hex mappings.
2. For each mutation v1..v4, emits a temp SVG with the `<style>` block's class fills replaced by that mutation's palette.
3. Renders the temp SVG to `<id>_v<N>.png` via Inkscape CLI at 512×512.
4. Deletes the temp SVG.

This keeps one editable SVG per creature with all four palettes co-located in the source.

---

## Task 1: Fallback texture

Generates the gray "?" placeholder that `CreatureSpriteLoader` returns when a sprite is missing.

**Files:**
- Create: `KeeperLegacyGodot/Sprites/Creatures/_fallback.svg`
- Create: `KeeperLegacyGodot/Sprites/Creatures/_fallback.png`
- Create: `tools/render_fallback.py`

- [ ] **Step 1: Write the fallback SVG**

Create `KeeperLegacyGodot/Sprites/Creatures/_fallback.svg`:

```svg
<svg viewBox="0 0 512 512" width="512" height="512" xmlns="http://www.w3.org/2000/svg">
  <rect x="64" y="64" width="384" height="384" rx="48" fill="#888888" stroke="#555555" stroke-width="6"/>
  <text x="256" y="340" font-family="sans-serif" font-size="280" font-weight="700"
        text-anchor="middle" fill="#ffffff">?</text>
</svg>
```

- [ ] **Step 2: Write the one-shot render script**

Create `tools/render_fallback.py`:

```python
"""Render _fallback.svg → _fallback.png at 512×512 via Inkscape CLI."""
import shutil, subprocess, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SVG  = ROOT / "KeeperLegacyGodot/Sprites/Creatures/_fallback.svg"
PNG  = ROOT / "KeeperLegacyGodot/Sprites/Creatures/_fallback.png"

def find_inkscape():
    for c in [r"C:\Program Files\Inkscape\bin\inkscape.exe",
              r"C:\Program Files (x86)\Inkscape\bin\inkscape.exe"]:
        if Path(c).exists(): return c
    return shutil.which("inkscape")

def main():
    ink = find_inkscape()
    if not ink:
        sys.exit("ERROR: Inkscape not found. Install from https://inkscape.org")
    subprocess.run([ink, "--export-type=png", f"--export-filename={PNG}",
                    "--export-width=512", "--export-height=512", str(SVG)], check=True)
    print(f"OK -> {PNG}")

if __name__ == "__main__":
    main()
```

- [ ] **Step 3: Run the render script**

```powershell
python tools\render_fallback.py
```

Expected: `OK -> ...\_fallback.png` and the file exists at 512×512 PNG.

- [ ] **Step 4: Verify dimensions**

```powershell
python -c "from PIL import Image; i = Image.open(r'KeeperLegacyGodot/Sprites/Creatures/_fallback.png'); print(i.size, i.mode)"
```

Expected: `(512, 512) RGBA` (or RGB).

If PIL isn't installed: `pip install Pillow`.

- [ ] **Step 5: Verify only new files were added**

```powershell
git status --short
```

Expected: only `??` (untracked) lines for the three new files. No `M` lines.

- [ ] **Step 6: Commit**

```powershell
git add KeeperLegacyGodot/Sprites/Creatures/_fallback.svg KeeperLegacyGodot/Sprites/Creatures/_fallback.png tools/render_fallback.py
git commit -m "feat(sprites): add fallback creature texture"
```

---

## Task 2: CreatureSpriteLoader (TDD)

Pure-logic path resolution (TDD-able with NUnit) plus a thin Godot wrapper that loads the texture.

**Files:**
- Create: `KeeperLegacyGodot/Data/CreatureSpriteLoader.cs`
- Create: `KeeperLegacyGodot/Tests/CreatureSpriteLoaderTests.cs`

- [ ] **Step 1: Write the failing test**

Create `KeeperLegacyGodot/Tests/CreatureSpriteLoaderTests.cs`:

```csharp
// Tests/CreatureSpriteLoaderTests.cs
using NUnit.Framework;
using KeeperLegacy.Data;

namespace KeeperLegacy.Tests
{
    [TestFixture]
    public class CreatureSpriteLoaderTests
    {
        [Test]
        public void ResolveIdlePath_BuildsCanonicalPath()
        {
            var path = CreatureSpriteLoader.ResolveIdlePath("coralsprite", 0);
            Assert.That(path, Is.EqualTo("res://Sprites/Creatures/coralsprite/coralsprite_v1.png"));
        }

        [Test]
        public void ResolveIdlePath_OneIndexedSuffix()
        {
            // mutationIndex 0..3 → v1..v4 in the filename
            Assert.That(CreatureSpriteLoader.ResolveIdlePath("bedrock", 0), Does.EndWith("bedrock_v1.png"));
            Assert.That(CreatureSpriteLoader.ResolveIdlePath("bedrock", 1), Does.EndWith("bedrock_v2.png"));
            Assert.That(CreatureSpriteLoader.ResolveIdlePath("bedrock", 2), Does.EndWith("bedrock_v3.png"));
            Assert.That(CreatureSpriteLoader.ResolveIdlePath("bedrock", 3), Does.EndWith("bedrock_v4.png"));
        }

        [Test]
        public void ResolveIdlePath_ThrowsOnEmptyId()
        {
            Assert.Throws<System.ArgumentException>(
                () => CreatureSpriteLoader.ResolveIdlePath("", 0));
        }

        [Test]
        public void ResolveIdlePath_ThrowsOnNegativeIndex()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => CreatureSpriteLoader.ResolveIdlePath("coralsprite", -1));
        }

        [Test]
        public void ResolveIdlePath_ThrowsOnIndexAboveThree()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => CreatureSpriteLoader.ResolveIdlePath("coralsprite", 4));
        }

        [Test]
        public void FallbackPath_IsCanonical()
        {
            Assert.That(CreatureSpriteLoader.FallbackPath,
                Is.EqualTo("res://Sprites/Creatures/_fallback.png"));
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```powershell
dotnet test --filter "FullyQualifiedName~CreatureSpriteLoaderTests"
```

Expected: build error or 6 failing tests because `CreatureSpriteLoader` doesn't exist yet.

- [ ] **Step 3: Write the loader**

Create `KeeperLegacyGodot/Data/CreatureSpriteLoader.cs`:

```csharp
// Data/CreatureSpriteLoader.cs
// Resolves a creature ID + mutation index to its idle sprite path,
// loads the Texture2D from Godot, and falls back to a gray "?" if missing.

using System;
using Godot;

namespace KeeperLegacy.Data
{
    public static class CreatureSpriteLoader
    {
        public const string FallbackPath = "res://Sprites/Creatures/_fallback.png";

        /// <summary>
        /// Pure path resolution. Throws on invalid input.
        /// mutationIndex is 0-based; the resulting filename uses 1-based v1..v4.
        /// </summary>
        public static string ResolveIdlePath(string creatureId, int mutationIndex)
        {
            if (string.IsNullOrWhiteSpace(creatureId))
                throw new ArgumentException("creatureId must not be empty.", nameof(creatureId));
            if (mutationIndex < 0 || mutationIndex > 3)
                throw new ArgumentOutOfRangeException(
                    nameof(mutationIndex), mutationIndex, "Must be 0..3.");

            return $"res://Sprites/Creatures/{creatureId}/{creatureId}_v{mutationIndex + 1}.png";
        }

        /// <summary>
        /// Loads the idle texture for (creatureId, mutationIndex).
        /// Returns the fallback texture if the file doesn't exist.
        /// </summary>
        public static Texture2D LoadIdle(string creatureId, int mutationIndex)
        {
            var path = ResolveIdlePath(creatureId, mutationIndex);
            if (ResourceLoader.Exists(path))
            {
                var tex = GD.Load<Texture2D>(path);
                if (tex != null) return tex;
            }
            return GD.Load<Texture2D>(FallbackPath);
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```powershell
dotnet test --filter "FullyQualifiedName~CreatureSpriteLoaderTests"
```

Expected: 6/6 passing.

- [ ] **Step 5: Run the full test suite to verify nothing else broke**

```powershell
dotnet test
```

Expected: all existing tests still pass plus the 6 new ones.

- [ ] **Step 6: Verify only new files added**

```powershell
git status --short
```

Expected: only `??` for `Data/CreatureSpriteLoader.cs` and `Tests/CreatureSpriteLoaderTests.cs`. No `M` lines.

- [ ] **Step 7: Commit**

```powershell
git add KeeperLegacyGodot/Data/CreatureSpriteLoader.cs KeeperLegacyGodot/Tests/CreatureSpriteLoaderTests.cs
git commit -m "feat(sprites): add CreatureSpriteLoader with fallback resolution"
```

---

## Task 3: Build script (`render_creature_sprites.py`)

Walks `Sprites/Creatures/*/`, for each `<id>.svg` parses the PALETTE comment, generates 4 mutation-colored temp SVGs, renders each to PNG via Inkscape, deletes the temp SVGs.

**Files:**
- Create: `tools/render_creature_sprites.py`
- Create: `tools/test_render_pipeline.py`
- Create: `tools/_fixtures/sample.svg` (a tiny test fixture)

- [ ] **Step 1: Create the test fixture SVG**

Create `tools/_fixtures/sample.svg`:

```svg
<svg viewBox="0 0 512 512" width="512" height="512" xmlns="http://www.w3.org/2000/svg">
  <!-- PALETTE
       v1: body-base=#ff0000 body-shadow=#800000
       v2: body-base=#00ff00 body-shadow=#008000
       v3: body-base=#0000ff body-shadow=#000080
       v4: body-base=#ffff00 body-shadow=#808000
  -->
  <style>
    .body-base   { fill: #ff0000; }
    .body-shadow { fill: #800000; }
  </style>
  <circle cx="256" cy="256" r="180" class="body-base" />
  <circle cx="256" cy="380" r="40"  class="body-shadow" />
</svg>
```

- [ ] **Step 2: Write the failing pipeline test**

Create `tools/test_render_pipeline.py`:

```python
"""Tests for render_creature_sprites.py — palette parsing + style rewriting."""
from pathlib import Path
import pytest
from render_creature_sprites import parse_palette, apply_palette

FIXTURE = Path(__file__).parent / "_fixtures" / "sample.svg"


def test_parse_palette_returns_four_mutations():
    svg = FIXTURE.read_text(encoding="utf-8")
    palettes = parse_palette(svg)
    assert set(palettes.keys()) == {1, 2, 3, 4}


def test_parse_palette_extracts_class_color_pairs():
    svg = FIXTURE.read_text(encoding="utf-8")
    palettes = parse_palette(svg)
    assert palettes[1]["body-base"]   == "#ff0000"
    assert palettes[1]["body-shadow"] == "#800000"
    assert palettes[2]["body-base"]   == "#00ff00"
    assert palettes[4]["body-shadow"] == "#808000"


def test_apply_palette_rewrites_class_fills():
    svg = FIXTURE.read_text(encoding="utf-8")
    out = apply_palette(svg, {"body-base": "#123456", "body-shadow": "#abcdef"})
    assert ".body-base   { fill: #123456; }" in out or ".body-base { fill: #123456; }" in out
    assert "#ff0000" not in out  # original v1 default replaced
    assert "#800000" not in out


def test_apply_palette_leaves_other_content_intact():
    svg = FIXTURE.read_text(encoding="utf-8")
    out = apply_palette(svg, {"body-base": "#123456", "body-shadow": "#abcdef"})
    assert '<circle cx="256" cy="256" r="180" class="body-base"' in out
    assert 'viewBox="0 0 512 512"' in out
```

- [ ] **Step 3: Run the test to verify it fails**

```powershell
cd tools
python -m pytest test_render_pipeline.py -v
```

Expected: ImportError because `render_creature_sprites.py` doesn't exist.

If pytest isn't installed: `pip install pytest`.

- [ ] **Step 4: Implement the build script**

Create `tools/render_creature_sprites.py`:

```python
"""
render_creature_sprites.py
Walks KeeperLegacyGodot/Sprites/Creatures/*/, for each <id>.svg:
  1. Parses the PALETTE comment (4 mutations, class -> hex color).
  2. Generates 4 temp SVGs with class fills overridden per mutation.
  3. Renders each to <id>_v<N>.png at 512x512 via Inkscape CLI.

Usage:
  python tools/render_creature_sprites.py            # render all
  python tools/render_creature_sprites.py coralsprite # render one
"""
import argparse, re, shutil, subprocess, sys, tempfile
from pathlib import Path

ROOT          = Path(__file__).resolve().parent.parent
CREATURES_DIR = ROOT / "KeeperLegacyGodot" / "Sprites" / "Creatures"
OUTPUT_SIZE   = 512

# ── Inkscape detection ─────────────────────────────────────────────────────────

def find_inkscape() -> str | None:
    for c in [r"C:\Program Files\Inkscape\bin\inkscape.exe",
              r"C:\Program Files (x86)\Inkscape\bin\inkscape.exe"]:
        if Path(c).exists(): return c
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
        if not line: continue
        m = MUTATION_LINE_RE.match(line)
        if not m: continue
        mutation = int(m.group(1))
        palettes[mutation] = dict(KV_RE.findall(m.group(2)))
    if set(palettes.keys()) != {1, 2, 3, 4}:
        raise ValueError(f"PALETTE must define v1..v4, got {sorted(palettes.keys())}")
    return palettes

# ── Style rewriting ────────────────────────────────────────────────────────────

CLASS_RULE_RE = re.compile(r"(\.[a-z][a-z0-9-]*\s*\{\s*fill:\s*)#[0-9a-fA-F]{3,6}(\s*;\s*\})")

def apply_palette(svg_text: str, palette: dict[str, str]) -> str:
    """Rewrite each `.class { fill: #...; }` rule with the palette's color."""
    def replace(match: re.Match) -> str:
        prefix, suffix = match.group(1), match.group(2)
        # Class name: between the leading "." and the first whitespace/{
        cls = re.match(r"\.([a-z][a-z0-9-]*)", prefix).group(1)
        color = palette.get(cls)
        if color is None:
            return match.group(0)  # no override for this class
        return f"{prefix}{color}{suffix}"
    return CLASS_RULE_RE.sub(replace, svg_text)

# ── Rendering ──────────────────────────────────────────────────────────────────

def render_svg_to_png(inkscape: str, svg_path: Path, png_path: Path):
    cmd = [inkscape, "--export-type=png", f"--export-filename={png_path}",
           f"--export-width={OUTPUT_SIZE}", f"--export-height={OUTPUT_SIZE}",
           str(svg_path)]
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        raise RuntimeError(f"Inkscape failed: {result.stderr.strip()}")

def render_creature(inkscape: str, creature_dir: Path, pilot_render: bool = False):
    creature_id = creature_dir.name
    svg_path    = creature_dir / f"{creature_id}.svg"
    if not svg_path.exists():
        print(f"  SKIP: {svg_path} not found")
        return
    out_dir = creature_dir / "_svg_pilot" if pilot_render else creature_dir
    out_dir.mkdir(exist_ok=True)
    svg_text = svg_path.read_text(encoding="utf-8")
    palettes = parse_palette(svg_text)
    for mutation in (1, 2, 3, 4):
        out_svg = apply_palette(svg_text, palettes[mutation])
        png_path = out_dir / f"{creature_id}_v{mutation}.png"
        with tempfile.NamedTemporaryFile(suffix=".svg", delete=False, mode="w", encoding="utf-8") as tmp:
            tmp.write(out_svg)
            tmp_path = Path(tmp.name)
        try:
            render_svg_to_png(inkscape, tmp_path, png_path)
            rel = png_path.relative_to(creature_dir.parent.parent.parent)
            print(f"  OK -> {rel}")
        finally:
            tmp_path.unlink(missing_ok=True)

# ── Main ───────────────────────────────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("creature", nargs="?", help="Creature ID (omit for all)")
    parser.add_argument("--pilot-render", action="store_true",
                        help="Render into <id>/_svg_pilot/ instead of overwriting canonical PNGs. "
                             "Use for creatures with illustrated migrations (coralsprite, deepecho).")
    args = parser.parse_args()

    inkscape = find_inkscape()
    if not inkscape:
        sys.exit("ERROR: Inkscape not found. Install from https://inkscape.org")

    targets = ([CREATURES_DIR / args.creature] if args.creature
               else sorted(p for p in CREATURES_DIR.iterdir() if p.is_dir() and not p.name.startswith("_")))

    for d in targets:
        if not d.is_dir():
            print(f"SKIP: {d} not a directory")
            continue
        print(d.name)
        render_creature(inkscape, d, pilot_render=args.pilot_render)

if __name__ == "__main__":
    main()
```

- [ ] **Step 5: Run pipeline tests to verify they pass**

```powershell
cd tools
python -m pytest test_render_pipeline.py -v
```

Expected: 4/4 passing.

- [ ] **Step 6: Smoke-test the script against the fixture**

```powershell
# Temporarily place the fixture as a creature so we can render it end-to-end
mkdir KeeperLegacyGodot\Sprites\Creatures\_smoketest
copy tools\_fixtures\sample.svg KeeperLegacyGodot\Sprites\Creatures\_smoketest\_smoketest.svg
python tools\render_creature_sprites.py _smoketest
```

Expected output: four lines `OK -> _smoketest_v1.png` … `_v4.png`. Open `_smoketest_v1.png` through `_v4.png` and confirm visible color difference (red, green, blue, yellow circles).

- [ ] **Step 7: Clean up the smoke-test directory**

```powershell
Remove-Item -Recurse -Force KeeperLegacyGodot\Sprites\Creatures\_smoketest
```

Expected: the folder is gone. `git status --short` shows no new files from the smoketest.

- [ ] **Step 8: Commit**

```powershell
git add tools/render_creature_sprites.py tools/test_render_pipeline.py tools/_fixtures/sample.svg
git commit -m "feat(sprites): add SVG→PNG render pipeline with palette injection"
```

---

## Task 4: Migrate the 3 illustrated PNGs

The existing `CharacterModels/Coralsprite|Deepecho|Tidecaller` PNGs become canonical idle sprites. Backgrounds are removed, files renamed to the canonical scheme, copies placed under `Sprites/Creatures/<id>/`. Originals stay in `CharacterModels/`.

**Files:**
- Create: 12 PNGs under `Sprites/Creatures/{coralsprite,deepecho,tidecaller}/`
- Create: `tools/migrate_illustrated.py`

**Mutation order** (read from `KeeperLegacyGodot/Data/CreatureRosterData.cs:59` for Coralsprite, line 57 for Deepecho, line 58 for Tidecaller):

| Creature    | v1 (idx 0)        | v2 (idx 1)           | v3 (idx 2)        | v4 (idx 3)     |
|-------------|-------------------|----------------------|-------------------|----------------|
| coralsprite | VividOrange.png   | PinkCoral.png        | PaleYellow.png    | Purple.png     |
| deepecho    | AbyssBlack.png    | BioluminescentBlue.png | DeepPurple.png  | Emerald.png    |
| tidecaller  | StormGrey.png     | Seafoam.png          | SandyTan.png      | SlateBlue.png  |

- [ ] **Step 1: Install the background removal library**

```powershell
pip install rembg pillow
```

`rembg` runs an ONNX model locally — no API key required, no network at inference time after first download.

- [ ] **Step 2: Write the migration script**

Create `tools/migrate_illustrated.py`:

```python
"""
migrate_illustrated.py
Copies the 3 illustrated PNGs from CharacterModels/ into the canonical
Sprites/Creatures/<id>/ structure with backgrounds removed and 1-indexed names.
"""
from pathlib import Path
from PIL import Image
from rembg import remove

ROOT  = Path(__file__).resolve().parent.parent
SRC   = ROOT / "CharacterModels"
DST   = ROOT / "KeeperLegacyGodot" / "Sprites" / "Creatures"

# id -> ordered list of source filenames matching mutation index 0..3
MAPPING = {
    "coralsprite": ["VividOrange.png", "PinkCoral.png", "PaleYellow.png", "Purple.png"],
    "deepecho":    ["AbyssBlack.png",  "BioluminescentBlue.png", "DeepPurple.png", "Emerald.png"],
    "tidecaller":  ["StormGrey.png",   "Seafoam.png",  "SandyTan.png",   "SlateBlue.png"],
}

def main():
    for creature_id, source_names in MAPPING.items():
        out_dir = DST / creature_id
        out_dir.mkdir(parents=True, exist_ok=True)
        for idx, name in enumerate(source_names):
            src_file = SRC / creature_id.capitalize() / name
            dst_file = out_dir / f"{creature_id}_v{idx+1}.png"
            if not src_file.exists():
                print(f"  MISS: {src_file} not found, skipping {dst_file.name}")
                continue
            print(f"  {src_file.name} -> {dst_file.relative_to(ROOT)}")
            with src_file.open("rb") as f:
                cut = remove(f.read())
            # Ensure RGBA, save
            img = Image.open(__import__('io').BytesIO(cut)).convert("RGBA")
            img.save(dst_file)

if __name__ == "__main__":
    main()
```

- [ ] **Step 3: Run the migration**

```powershell
python tools\migrate_illustrated.py
```

Expected: 12 lines `Coralsprite/VividOrange.png -> KeeperLegacyGodot/Sprites/Creatures/coralsprite/coralsprite_v1.png`, etc. (one per mutation per creature).

- [ ] **Step 4: Visually verify each output**

Open each of the 12 PNGs and confirm:
- The creature has a transparent (checkered) background.
- The creature silhouette is clean — no leftover gradient halo or chunks of the original gray backdrop.
- Image dimensions are roughly the same as the original (rembg doesn't crop).

If any image has significant background-removal artifacts (haloing, cut-off limbs), note which creature/mutation in the commit message and treat it as a known issue — the placeholder SVG will eventually replace it for that mutation.

- [ ] **Step 5: Verify only new files added**

```powershell
git status --short
```

Expected: 12 `??` lines under `KeeperLegacyGodot/Sprites/Creatures/{coralsprite,deepecho,tidecaller}/` plus `tools/migrate_illustrated.py`. No `M` lines anywhere.

- [ ] **Step 6: Commit**

```powershell
git add tools/migrate_illustrated.py KeeperLegacyGodot/Sprites/Creatures/coralsprite KeeperLegacyGodot/Sprites/Creatures/deepecho KeeperLegacyGodot/Sprites/Creatures/tidecaller
git commit -m "feat(sprites): migrate 3 illustrated creatures into canonical paths"
```

---

## Task 5: Pilot — Coralsprite SVG

First pilot. Will be **rendered and shown alongside the existing illustrated PNG** for direct quality comparison.

**Reference description** (from `KeeperLegacyGodot/Data/CreatureRosterData.cs:59` and `SpriteSheets/ProductionBatches.txt`):
> "Lives among coral reefs and can grow tiny coral formations from its fingertips."
> Small, lively round creature with tiny coral formations growing from its fingertips and head. Wide grin always visible. Round body, large expressive eyes. Short legs with round foot pads. The coral formations on the head are the defining feature — small branching antler-like coral structures.

**Mutations** (from `CreatureRosterData.cs:59`):
- v1: Vivid Orange
- v2: Pink Coral
- v3: Pale Yellow
- v4: Purple

**Files:**
- Create: `KeeperLegacyGodot/Sprites/Creatures/coralsprite/coralsprite.svg`
- Create: `KeeperLegacyGodot/Sprites/Creatures/coralsprite/_svg_pilot/coralsprite_v{1..4}.png` (rendered via `--pilot-render`)

The illustrated PNGs at `coralsprite/coralsprite_v{1..4}.png` (created in Task 4) **remain canonical and are not overwritten**. SVG renders go to the `_svg_pilot/` subfolder for style review only.

- [ ] **Step 1: Author `coralsprite.svg`**

Create `KeeperLegacyGodot/Sprites/Creatures/coralsprite/coralsprite.svg`. The SVG must:

1. `viewBox="0 0 512 512"`, `width="512"`, `height="512"`, no root background rect.
2. PALETTE comment with all 4 mutations using these colors (consult the reference illustrated PNGs and pick hex values that match):
   - v1 (Vivid Orange): warm orange body (~`#f06820`), darker orange shadow (~`#903000`), brighter orange highlight (~`#ff9040`), red-orange coral accent (~`#ff5020`).
   - v2 (Pink Coral): warm pink body, deep rose shadow, blush highlight, coral-pink accent.
   - v3 (Pale Yellow): soft buttery body, tan shadow, cream highlight, peach accent.
   - v4 (Purple): mid-violet body, plum shadow, lavender highlight, magenta accent.
3. `<style>` block with classes for every visually distinct surface — at minimum: `.body-base`, `.body-shadow`, `.body-highlight`, `.body-stroke`, `.accent-base`, `.accent-stroke`, `.eye-white`, `.eye-pupil`, `.mouth`. Eyes are NOT part of the per-mutation palette — they're constant across mutations (dark pupil, white sclera).
4. Creature artwork using class-based fills/strokes only — no inline `fill="#..."` for any element that should change color per mutation. Inline fills are OK only for elements that are constant across mutations (eyes, mouth interior).
5. Quality bar from spec section "Visual quality bar": distinctive silhouette, 3-tone shading on body, large expressive eyes, friendly grin, coral antlers prominent on head, coral nubs on fingertips, ~80% canvas occupancy, vertically centered with slight downward bias (room for happy-bounce).

Skeleton to start from:

```svg
<svg viewBox="0 0 512 512" width="512" height="512" xmlns="http://www.w3.org/2000/svg">
  <!-- PALETTE
       v1: body-base=#f06820 body-shadow=#903000 body-highlight=#ff9040 body-stroke=#5a1f00 accent-base=#ff5020 accent-stroke=#7a2000
       v2: body-base=#ff80a0 body-shadow=#a04060 body-highlight=#ffb0c0 body-stroke=#5a1030 accent-base=#e04060 accent-stroke=#6a1030
       v3: body-base=#f5e070 body-shadow=#a89030 body-highlight=#fff0a0 body-stroke=#5a4a10 accent-base=#f0c050 accent-stroke=#6a5010
       v4: body-base=#a070d0 body-shadow=#503080 body-highlight=#c8a8e8 body-stroke=#2a1050 accent-base=#7040b0 accent-stroke=#3a1050
  -->
  <style>
    .body-base      { fill: #f06820; }
    .body-shadow    { fill: #903000; }
    .body-highlight { fill: #ff9040; }
    .body-stroke    { fill: #5a1f00; }
    .accent-base    { fill: #ff5020; }
    .accent-stroke  { fill: #7a2000; }
    .eye-white      { fill: #ffffff; }
    .eye-pupil      { fill: #2a1500; }
    .mouth          { fill: #ffd0d0; }
  </style>

  <!-- creature artwork — body, legs, arms, coral head antlers, coral fingertip nubs, eyes, mouth -->
  <!-- Author this section. ~80% canvas, centered, vertically biased downward for bounce headroom. -->
</svg>
```

- [ ] **Step 2: Render the pilot into `_svg_pilot/`**

```powershell
python tools\render_creature_sprites.py coralsprite --pilot-render
```

Expected: four lines `OK -> KeeperLegacyGodot/Sprites/Creatures/coralsprite/_svg_pilot/coralsprite_v1.png` through `_v4.png`. The canonical `coralsprite/coralsprite_v{1..4}.png` (illustrated) are unchanged.

- [ ] **Step 3: Visual self-check against quality bar**

Open all 4 SVG-rendered PNGs in `coralsprite/_svg_pilot/` side by side with the 4 illustrated `coralsprite/coralsprite_v{1..4}.png` files. Check the SVG renders against the spec's quality bar:

- [ ] Distinctive silhouette (could you identify this as Coralsprite from silhouette alone?)
- [ ] 3-tone shading visible on body (not flat)
- [ ] Coral head antlers prominent
- [ ] Eyes large and expressive
- [ ] Mouth friendly/cute
- [ ] All 4 mutations visually different (color only — same shape/pose)
- [ ] Eyes are identical across all 4 mutations
- [ ] Creature occupies ~80% of frame, slightly below center

If any check fails, iterate on `coralsprite.svg` and re-render. Do not commit until all 8 checks pass.

- [ ] **Step 4: Verify only new files added**

```powershell
git status --short
```

Expected: `??` for `coralsprite.svg` and the 4 files in `_svg_pilot/`. **No `M` lines** anywhere — especially not against the canonical `coralsprite_v{1..4}.png` files (Task 4's illustrated migrations).

- [ ] **Step 5: Commit**

```powershell
git add KeeperLegacyGodot/Sprites/Creatures/coralsprite/coralsprite.svg KeeperLegacyGodot/Sprites/Creatures/coralsprite/_svg_pilot
git commit -m "feat(sprites): pilot SVG for coralsprite (style review against illustrated)"
```

---

## Task 6: Pilot — Bedrock SVG

Second pilot. Tests the style on a chunky, blocky, mineral-textured creature very different from Coralsprite.

**Reference description** (from `KeeperLegacyGodot/Data/CreatureRosterData.cs:77` and `SpriteSheets/ProductionBatches.txt` Batch 4):
> "The sturdiest of all dirt creatures — nothing can knock it over."
> Massive, squat, immovable-looking creature made of layered stone. Cracks of mineral color run across its surface.

**Mutations** (from `CreatureRosterData.cs:77`):
- v1: Granite Grey
- v2: Dark Brown
- v3: Iron Black
- v4: Sandy Tan

**Files:**
- Create: `KeeperLegacyGodot/Sprites/Creatures/bedrock/bedrock.svg`
- Create: `KeeperLegacyGodot/Sprites/Creatures/bedrock/bedrock_v{1..4}.png` (rendered)

- [ ] **Step 1: Author `bedrock.svg`**

Same skeleton as Task 5 step 2. Differences for Bedrock:

- Silhouette: wide, low, blocky. Almost trapezoidal body. Tiny stubby legs implied at base.
- Layered stone texture: 2-3 horizontal layer divisions across the body using `.body-shadow` strokes.
- Visible mineral cracks: thin lines in `.accent-base` color across the body (the "cracks of mineral color").
- Friendly face: same large eyes / grin as Coralsprite, but face occupies a smaller portion of the body since the body is wider.
- No appendages on top of head (contrast vs. Coralsprite's coral antlers).

PALETTE colors (pick hexes by eye to match the names):

```
v1: body-base=#9a9a9a body-shadow=#606060 body-highlight=#c8c8c8 body-stroke=#303030 accent-base=#5080b8 accent-stroke=#1a3060
v2: body-base=#6a4a30 body-shadow=#3a2818 body-highlight=#9a7050 body-stroke=#1a1008 accent-base=#c0a060 accent-stroke=#503010
v3: body-base=#3a3a3a body-shadow=#1a1a1a body-highlight=#5a5a5a body-stroke=#000000 accent-base=#806020 accent-stroke=#302000
v4: body-base=#d8c098 body-shadow=#9a7848 body-highlight=#f0e0c0 body-stroke=#503810 accent-base=#a8804a accent-stroke=#503000
```

CSS classes used: same set as Coralsprite (consistent across all creatures so the loader / palette parser code remains uniform).

- [ ] **Step 2: Render the pilot**

```powershell
python tools\render_creature_sprites.py bedrock
```

Expected: 4 PNGs created under `Sprites/Creatures/bedrock/`.

- [ ] **Step 3: Visual self-check**

- [ ] Silhouette unmistakably blocky / squat (not round). Compare against Coralsprite render — they should look like very different creatures.
- [ ] Layered stone visible (horizontal divisions on body).
- [ ] Mineral cracks visible in accent color.
- [ ] Face is friendly despite the heavy body.
- [ ] All 4 mutations differ in color only.
- [ ] Eyes consistent across mutations.
- [ ] Creature occupies ~80% of frame.

- [ ] **Step 4: Verify only new files added**

```powershell
git status --short
```

Expected: only `??` lines under `Sprites/Creatures/bedrock/` and `tools/`. No `M` lines against existing project files.

- [ ] **Step 5: Commit**

```powershell
git add KeeperLegacyGodot/Sprites/Creatures/bedrock
git commit -m "feat(sprites): pilot SVG for bedrock"
```

---

## Task 7: Pilot — Deepecho SVG

Third pilot. Tests the style on a dark, bioluminescent creature — checks how the palette/style approach handles glow/luminance effects in pure SVG.

**Reference description** (from `KeeperLegacyGodot/Data/CreatureRosterData.cs:57` and `SpriteSheets/ProductionBatches.txt`):
> "Born in the darkest ocean trenches, it communicates through haunting melodies."
> Dark, deep-sea creature with bioluminescent markings along its sides. Wide haunting eyes, no visible light source — it IS the light.

**Mutations** (from `CreatureRosterData.cs:57`):
- v1: Abyss Black
- v2: Bioluminescent Blue
- v3: Deep Purple
- v4: Emerald

**Files:**
- Create: `KeeperLegacyGodot/Sprites/Creatures/deepecho/deepecho.svg`
- Create: `KeeperLegacyGodot/Sprites/Creatures/deepecho/_svg_pilot/deepecho_v{1..4}.png`

The illustrated PNGs at `deepecho/deepecho_v{1..4}.png` (from Task 4) **remain canonical and are not overwritten**.

- [ ] **Step 1: Author `deepecho.svg`**

Differences from Coralsprite:
- Silhouette: fish-shaped, longer than tall. Tail prominent. A dangling lure (anglerfish-style) on a stalk above the head.
- Bioluminescent markings: dots and lines along the side of the body, rendered with a new class `.glow-base` that's bright (high-luminance) regardless of mutation. Add a soft outer glow via a duplicated shape with reduced opacity.
- Eyes: oversized, "haunting" — pupils slightly larger than Coralsprite's, with a tiny white highlight.
- Open mouth showing tiny pointed teeth (different mood than Coralsprite's grin).

Add **two new classes** to the palette and `<style>`: `.glow-base`, `.glow-stroke`. These are the bioluminescent dots — they are bright across all mutations (mutation only affects the body/accent colors, not the glow).

PALETTE colors:

```
v1: body-base=#0a0a18 body-shadow=#000000 body-highlight=#1a1a30 body-stroke=#000000 accent-base=#2a2a48 accent-stroke=#000000 glow-base=#a0e0ff glow-stroke=#80c0e0
v2: body-base=#1a3050 body-shadow=#0a1828 body-highlight=#3050a0 body-stroke=#000820 accent-base=#3060c0 accent-stroke=#102060 glow-base=#a0e0ff glow-stroke=#80c0e0
v3: body-base=#2a1050 body-shadow=#1a0830 body-highlight=#5028a0 body-stroke=#080020 accent-base=#5028a0 accent-stroke=#200840 glow-base=#e0a0ff glow-stroke=#c080e0
v4: body-base=#0a3a28 body-shadow=#001810 body-highlight=#208050 body-stroke=#001008 accent-base=#208050 accent-stroke=#001810 glow-base=#a0ffa0 glow-stroke=#80e080
```

- [ ] **Step 2: Render into `_svg_pilot/`**

```powershell
python tools\render_creature_sprites.py deepecho --pilot-render
```

Expected: four PNGs in `deepecho/_svg_pilot/`. Canonical illustrated PNGs unchanged.

- [ ] **Step 3: Visual self-check**

Compare `_svg_pilot/deepecho_v{1..4}.png` (SVG) against `deepecho/deepecho_v{1..4}.png` (illustrated):

- [ ] Body is dark; bioluminescent dots are bright and visible against the body.
- [ ] Soft glow halo around each dot (alpha-reduced duplicate shape).
- [ ] Silhouette is fish-shaped, not round (distinct from Coralsprite/Bedrock).
- [ ] Lure on stalk visible above the head.
- [ ] Eyes large and "haunting" — slightly oversized pupils.
- [ ] Glow color stays consistent within each variant but body color changes per mutation.

Iterate on `deepecho.svg` and re-render until all checks pass.

- [ ] **Step 4: Verify only new files added**

```powershell
git status --short
```

Expected: `??` for `deepecho.svg` and the 4 files in `_svg_pilot/`. No `M` lines.

- [ ] **Step 5: Commit**

```powershell
git add KeeperLegacyGodot/Sprites/Creatures/deepecho/deepecho.svg KeeperLegacyGodot/Sprites/Creatures/deepecho/_svg_pilot
git commit -m "feat(sprites): pilot SVG for deepecho with bioluminescent glow"
```

---

## Task 8: STOP — Pilot baseline review gate

**This is a hard human-review gate. Do not proceed to Task 9 without explicit user approval.**

Three pilot creatures (Coralsprite, Bedrock, Deepecho) with all four mutations each are now rendered. Present them to the user for review.

- [ ] **Step 1: Open all 12 pilot PNGs side-by-side and request review**

The 12 files:
- `KeeperLegacyGodot/Sprites/Creatures/coralsprite/coralsprite_v{1..4}.png`
- `KeeperLegacyGodot/Sprites/Creatures/bedrock/bedrock_v{1..4}.png`
- `KeeperLegacyGodot/Sprites/Creatures/deepecho/deepecho_v{1..4}.png`

Ask the user: "Do these three pilots match the quality bar you want for the placeholder phase? Anything to adjust before I sweep the remaining 77 creatures?"

- [ ] **Step 2: Iterate per user feedback**

If the user requests changes (add detail, simplify, change shading approach, fix the eye size, etc.):
1. Update the relevant pilot SVG(s).
2. Re-render via `python tools\render_creature_sprites.py <id>`.
3. Commit each iteration: `git commit -m "fix(sprites): pilot iteration — <what changed>"`.
4. Re-present to the user.

Loop until the user explicitly says "approved" or equivalent. **No mass production until then.**

- [ ] **Step 3: After approval, document the locked baseline**

Once the user approves, append a short note to the spec under "Pilot baseline":

```powershell
# (example — actual text depends on what got locked in)
```

Edit `docs/superpowers/specs/2026-05-04-character-sprite-pipeline-design.md` to add a `## Pilot baseline (locked YYYY-MM-DD)` section listing any agreed conventions (e.g. "all bodies use 3 horizontal shading layers", "eyes are 38px wide × 32px tall", "stroke widths range 4-6px") so the sweep tasks have an explicit standard to match.

This is the **only** edit to an existing file in the entire plan, and it's only to a doc the user owns — not to any code, scene, or data file.

- [ ] **Step 4: Commit the locked baseline note**

```powershell
git add docs/superpowers/specs/2026-05-04-character-sprite-pipeline-design.md
git commit -m "docs(sprites): lock pilot baseline conventions"
```

---

## Tasks 9-15: Habitat-by-habitat sweep

For each of the seven habitats below, the workflow is identical:

**For each creature in the habitat list:**

1. Look up the creature in `KeeperLegacyGodot/Data/CreatureRosterData.cs` to confirm: ID, description, 4 mutation color hints.
2. Look up the additional design notes in `SpriteSheets/ProductionBatches.txt` (signature features, posture, distinctive details).
3. Author `KeeperLegacyGodot/Sprites/Creatures/<id>/<id>.svg` using the locked-baseline conventions established in Task 8.3 — same `<style>` class set, same canvas dimensions, same anchor, eyes constant across mutations.
4. Run `python tools\render_creature_sprites.py <id>` and verify 4 PNGs are produced.
5. Self-check against quality bar (silhouette distinctive, 3-tone shading, signature feature prominent, mutations differ in color only).
6. After all creatures in the habitat are done: `git status --short` to verify only new files. Then commit the entire habitat as one commit.

**For creatures with illustrated migrations (Coralsprite/Deepecho/Tidecaller — already done in Task 4 + Tasks 5/7):** Tidecaller is the only creature for which we are NOT authoring an SVG yet, since the illustrated PNG covers it. Skip Tidecaller in Task 9.

### Task 9: Water sweep (12 remaining)

Water creatures in `CreatureRosterData.cs` (lines 54-68): aquaburst, shimmerstream, seraphine, deepecho ✓ (pilot, illustrated canonical), tidecaller ⊘ (illustrated only — no SVG needed), coralsprite ✓ (pilot, illustrated canonical), mistwalker, wavecrest, pearlescent, riptide, bubblesnout, kelpling, frostfin, swirlpool, luminara.

Author SVGs for: aquaburst, shimmerstream, seraphine, mistwalker, wavecrest, pearlescent, riptide, bubblesnout, kelpling, frostfin, swirlpool, luminara (12 creatures). These are sweep-track creatures with no illustrated migration, so SVG renders go directly to canonical paths (no `--pilot-render`).

- [ ] **Step 1: Author 12 SVGs (one per creature)** following the workflow above.
- [ ] **Step 2: Render the entire water habitat**

```powershell
python tools\render_creature_sprites.py
# (no creature arg = render all; script will skip dirs without an .svg, so safe)
```

- [ ] **Step 3: Quick visual review** of all 12 × 4 = 48 PNGs. Flag any that drift from the baseline; iterate before commit.
- [ ] **Step 4: Verify only new files**: `git status --short` shows `??` lines only.
- [ ] **Step 5: Commit**

```powershell
git add KeeperLegacyGodot/Sprites/Creatures/aquaburst KeeperLegacyGodot/Sprites/Creatures/shimmerstream KeeperLegacyGodot/Sprites/Creatures/seraphine KeeperLegacyGodot/Sprites/Creatures/mistwalker KeeperLegacyGodot/Sprites/Creatures/wavecrest KeeperLegacyGodot/Sprites/Creatures/pearlescent KeeperLegacyGodot/Sprites/Creatures/riptide KeeperLegacyGodot/Sprites/Creatures/bubblesnout KeeperLegacyGodot/Sprites/Creatures/kelpling KeeperLegacyGodot/Sprites/Creatures/frostfin KeeperLegacyGodot/Sprites/Creatures/swirlpool KeeperLegacyGodot/Sprites/Creatures/luminara
git commit -m "feat(sprites): water habitat sweep (12 creatures)"
```

- [ ] **Step 6: Pause for user spot-check** before continuing to dirt.

### Task 10: Dirt sweep (14 remaining)

Dirt creatures in `CreatureRosterData.cs` (lines 75-89): crumblebane, dustdevil, bedrock ✓, stonework, mudbubble, terraclaw, sandwhistle, rootbound, gravelgrip, claymold, pebblesnap, moundmaker, tunnelworm, quartzling, geoheart.

Author SVGs for the 14 unmarked creatures.

- [ ] **Step 1**: Author 14 SVGs.
- [ ] **Step 2**: Render: `python tools\render_creature_sprites.py`
- [ ] **Step 3**: Visual review.
- [ ] **Step 4**: `git status --short` — only new files.
- [ ] **Step 5**: Commit:

```powershell
git add KeeperLegacyGodot/Sprites/Creatures/crumblebane KeeperLegacyGodot/Sprites/Creatures/dustdevil KeeperLegacyGodot/Sprites/Creatures/stonework KeeperLegacyGodot/Sprites/Creatures/mudbubble KeeperLegacyGodot/Sprites/Creatures/terraclaw KeeperLegacyGodot/Sprites/Creatures/sandwhistle KeeperLegacyGodot/Sprites/Creatures/rootbound KeeperLegacyGodot/Sprites/Creatures/gravelgrip KeeperLegacyGodot/Sprites/Creatures/claymold KeeperLegacyGodot/Sprites/Creatures/pebblesnap KeeperLegacyGodot/Sprites/Creatures/moundmaker KeeperLegacyGodot/Sprites/Creatures/tunnelworm KeeperLegacyGodot/Sprites/Creatures/quartzling KeeperLegacyGodot/Sprites/Creatures/geoheart
git commit -m "feat(sprites): dirt habitat sweep (14 creatures)"
```

- [ ] **Step 6**: User spot-check.

### Task 11: Grass sweep (15)

Grass creatures (lines 96-110): wildbloom, blossom, photosynthese, chlorophyll, vinetwist, meadowpuff, thornback, fernwhisper, seedling, mossback, pollencloud, leafdancer, rootweaver, sproutling, verdantheart.

Same workflow. Final commit:

```powershell
git add KeeperLegacyGodot/Sprites/Creatures/wildbloom KeeperLegacyGodot/Sprites/Creatures/blossom KeeperLegacyGodot/Sprites/Creatures/photosynthese KeeperLegacyGodot/Sprites/Creatures/chlorophyll KeeperLegacyGodot/Sprites/Creatures/vinetwist KeeperLegacyGodot/Sprites/Creatures/meadowpuff KeeperLegacyGodot/Sprites/Creatures/thornback KeeperLegacyGodot/Sprites/Creatures/fernwhisper KeeperLegacyGodot/Sprites/Creatures/seedling KeeperLegacyGodot/Sprites/Creatures/mossback KeeperLegacyGodot/Sprites/Creatures/pollencloud KeeperLegacyGodot/Sprites/Creatures/leafdancer KeeperLegacyGodot/Sprites/Creatures/rootweaver KeeperLegacyGodot/Sprites/Creatures/sproutling KeeperLegacyGodot/Sprites/Creatures/verdantheart
git commit -m "feat(sprites): grass habitat sweep (15 creatures)"
```

### Task 12: Fire sweep (10)

Fire creatures (lines 117-126): cinderborne, scorchwhirl, flamewing, volatile, emberpaw, sparksnout, magmakin, ashwalker, solarflare, infernokin.

Final commit:

```powershell
git add KeeperLegacyGodot/Sprites/Creatures/cinderborne KeeperLegacyGodot/Sprites/Creatures/scorchwhirl KeeperLegacyGodot/Sprites/Creatures/flamewing KeeperLegacyGodot/Sprites/Creatures/volatile KeeperLegacyGodot/Sprites/Creatures/emberpaw KeeperLegacyGodot/Sprites/Creatures/sparksnout KeeperLegacyGodot/Sprites/Creatures/magmakin KeeperLegacyGodot/Sprites/Creatures/ashwalker KeeperLegacyGodot/Sprites/Creatures/solarflare KeeperLegacyGodot/Sprites/Creatures/infernokin
git commit -m "feat(sprites): fire habitat sweep (10 creatures)"
```

### Task 13: Ice sweep (10)

Ice creatures (lines 133-142): frostveil, frostbite, tundraform, blizzardborne, snowpuff, crystalmane, permafrost, hailstone, glaciercalve, aurorakin.

Final commit:

```powershell
git add KeeperLegacyGodot/Sprites/Creatures/frostveil KeeperLegacyGodot/Sprites/Creatures/frostbite KeeperLegacyGodot/Sprites/Creatures/tundraform KeeperLegacyGodot/Sprites/Creatures/blizzardborne KeeperLegacyGodot/Sprites/Creatures/snowpuff KeeperLegacyGodot/Sprites/Creatures/crystalmane KeeperLegacyGodot/Sprites/Creatures/permafrost KeeperLegacyGodot/Sprites/Creatures/hailstone KeeperLegacyGodot/Sprites/Creatures/glaciercalve KeeperLegacyGodot/Sprites/Creatures/aurorakin
git commit -m "feat(sprites): ice habitat sweep (10 creatures)"
```

### Task 14: Electric sweep (10)

Electric creatures (lines 149-158): sparkburst, voltspire, electra, luminant, thunderpup, staticfur, arcdancer, stormcaller, thunderheart, zenithstrike.

Final commit:

```powershell
git add KeeperLegacyGodot/Sprites/Creatures/sparkburst KeeperLegacyGodot/Sprites/Creatures/voltspire KeeperLegacyGodot/Sprites/Creatures/electra KeeperLegacyGodot/Sprites/Creatures/luminant KeeperLegacyGodot/Sprites/Creatures/thunderpup KeeperLegacyGodot/Sprites/Creatures/staticfur KeeperLegacyGodot/Sprites/Creatures/arcdancer KeeperLegacyGodot/Sprites/Creatures/stormcaller KeeperLegacyGodot/Sprites/Creatures/thunderheart KeeperLegacyGodot/Sprites/Creatures/zenithstrike
git commit -m "feat(sprites): electric habitat sweep (10 creatures)"
```

### Task 15: Magical sweep (5)

Magical creatures (lines 165-169): arcane, constellation, infinity, divinity, cosmicwarden.

These are the rarest creatures — feel free to give them slightly more elaborate detail (extra accent classes, more particle/star elements) within the locked baseline.

Final commit:

```powershell
git add KeeperLegacyGodot/Sprites/Creatures/arcane KeeperLegacyGodot/Sprites/Creatures/constellation KeeperLegacyGodot/Sprites/Creatures/infinity KeeperLegacyGodot/Sprites/Creatures/divinity KeeperLegacyGodot/Sprites/Creatures/cosmicwarden
git commit -m "feat(sprites): magical habitat sweep (5 creatures)"
```

---

## Task 16: Final completeness verification

**Files:** none created — verification only.

- [ ] **Step 1: Verify every creature ID has 4 PNGs**

Create a one-shot verification script as a PowerShell snippet (don't commit it):

```powershell
$ids = @("aquaburst","shimmerstream","seraphine","deepecho","tidecaller","coralsprite","mistwalker","wavecrest","pearlescent","riptide","bubblesnout","kelpling","frostfin","swirlpool","luminara","crumblebane","dustdevil","bedrock","stonework","mudbubble","terraclaw","sandwhistle","rootbound","gravelgrip","claymold","pebblesnap","moundmaker","tunnelworm","quartzling","geoheart","wildbloom","blossom","photosynthese","chlorophyll","vinetwist","meadowpuff","thornback","fernwhisper","seedling","mossback","pollencloud","leafdancer","rootweaver","sproutling","verdantheart","cinderborne","scorchwhirl","flamewing","volatile","emberpaw","sparksnout","magmakin","ashwalker","solarflare","infernokin","frostveil","frostbite","tundraform","blizzardborne","snowpuff","crystalmane","permafrost","hailstone","glaciercalve","aurorakin","sparkburst","voltspire","electra","luminant","thunderpup","staticfur","arcdancer","stormcaller","thunderheart","zenithstrike","arcane","constellation","infinity","divinity","cosmicwarden")
$missing = @()
foreach ($id in $ids) {
    foreach ($v in 1..4) {
        $p = "KeeperLegacyGodot\Sprites\Creatures\$id\${id}_v$v.png"
        if (-not (Test-Path $p)) { $missing += $p }
    }
}
if ($missing.Count -eq 0) { "All 320 PNGs present." } else { "MISSING:"; $missing }
```

Expected: `All 320 PNGs present.`

If any are missing, return to the relevant habitat task and resolve.

- [ ] **Step 2: Verify the loader can resolve every (id, mutationIndex) pair**

Add a single new test to `KeeperLegacyGodot/Tests/CreatureSpriteLoaderTests.cs`:

```csharp
[Test]
public void ResolveIdlePath_CoversEveryCreatureMutation()
{
    foreach (var c in KeeperLegacy.Data.CreatureRosterData.AllCreatures)
        for (int i = 0; i < c.Mutations.Count; i++)
            Assert.That(CreatureSpriteLoader.ResolveIdlePath(c.Id, i), Does.StartWith("res://Sprites/Creatures/"));
}
```

Run: `dotnet test --filter "FullyQualifiedName~ResolveIdlePath_CoversEveryCreatureMutation"` — expect PASS.

- [ ] **Step 3: Run the full test suite**

```powershell
dotnet test
```

Expected: all existing tests + new sprite tests pass.

- [ ] **Step 4: Final git status check**

```powershell
git status --short
```

Expected: clean. All work is committed.

- [ ] **Step 5: Commit the new test**

```powershell
git add KeeperLegacyGodot/Tests/CreatureSpriteLoaderTests.cs
git commit -m "test(sprites): verify loader resolves every creature/mutation"
```

---

## Done

All 80 creatures have 4 mutation PNGs each (320 total). `CreatureSpriteLoader` resolves any `(creatureId, mutationIndex)` pair to a Texture2D, with a fallback for any missing file. The build pipeline is reproducible (`python tools\render_creature_sprites.py`). Zero existing files in `KeeperLegacyGodot/`, `JSON Design/`, or `SpriteSheets/` were modified — the parallel scene-development session is unaffected.

Future work (out of scope for this plan): wire `CreatureSpriteLoader.LoadIdle` into `PedestalNode` and other UI in the scene-development session; build procedural Interact / Happy / Sad animations on top of the idle sprites; replace placeholder SVGs with hand-illustrated PNGs creature-by-creature as production approaches.
