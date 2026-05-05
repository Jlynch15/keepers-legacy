"""Tests for render_creature_sprites.py — palette parsing + style rewriting."""
from pathlib import Path
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
    # The active <style> rules now use the palette colors
    assert ".body-base" in out and "#123456" in out
    assert ".body-shadow" in out and "#abcdef" in out
    # PALETTE comment retains its metadata (intentional — it's the source map for all 4 mutations)
    assert "v1: body-base=#ff0000" in out


def test_apply_palette_leaves_other_content_intact():
    svg = FIXTURE.read_text(encoding="utf-8")
    out = apply_palette(svg, {"body-base": "#123456", "body-shadow": "#abcdef"})
    assert '<circle cx="256" cy="256" r="180" class="body-base"' in out
    assert 'viewBox="0 0 512 512"' in out
