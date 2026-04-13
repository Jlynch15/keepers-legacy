"""
fix_arcane_text.py
Replaces <text> elements in arcane_sheet.svg with equivalent SVG paths.
  ✦  → 4-pointed star path
  ⬡  → hexagon polygon (outline)

Run: python fix_arcane_text.py
Output: arcane_sheet.svg is updated in-place (original backed up as arcane_sheet_backup.svg)
"""

import re, math, shutil

INPUT  = "arcane_sheet.svg"
BACKUP = "arcane_sheet_backup.svg"

# ── Helpers ──────────────────────────────────────────────────────────────────

def star_path(cx, cy, outer_r, inner_r, fill, opacity):
    """4-pointed star (✦). Points at N/E/S/W with inner concave corners at 45°."""
    r, ir = outer_r, inner_r
    d = (f"M {cx:.2f},{cy-r:.2f} "
         f"L {cx+ir:.2f},{cy-ir:.2f} "
         f"L {cx+r:.2f},{cy:.2f} "
         f"L {cx+ir:.2f},{cy+ir:.2f} "
         f"L {cx:.2f},{cy+r:.2f} "
         f"L {cx-ir:.2f},{cy+ir:.2f} "
         f"L {cx-r:.2f},{cy:.2f} "
         f"L {cx-ir:.2f},{cy-ir:.2f} Z")
    return f'<path d="{d}" fill="{fill}" opacity="{opacity}"/>'


def hexagon_polygon(cx, cy, r, fill, opacity):
    """Flat-top regular hexagon outline (⬡ = WHITE HEXAGON = outline style)."""
    pts = []
    for i in range(6):
        angle = math.radians(i * 60)           # flat-top: first point at 0° (right)
        pts.append(f"{cx + r*math.cos(angle):.2f},{cy + r*math.sin(angle):.2f}")
    return (f'<polygon points="{" ".join(pts)}" '
            f'fill="none" stroke="{fill}" stroke-width="1" opacity="{opacity}"/>')


def text_to_shape(x_str, y_str, size_str, fill, opacity, char):
    """Convert a <text> element's attributes to the equivalent shape element."""
    x    = float(x_str)
    y    = float(y_str)
    size = float(size_str)

    # Adjust from text baseline to visual center of the glyph
    cy = y - size * 0.46

    if char == "✦":
        outer_r = size * 0.47
        inner_r = size * 0.13
        return star_path(x, cy, outer_r, inner_r, fill, opacity)
    elif char == "⬡":
        r = size * 0.46
        return hexagon_polygon(x, cy, r, fill, opacity)
    else:
        return None   # Leave unknown chars alone


# ── Regex ─────────────────────────────────────────────────────────────────────
# Matches: <text x="N" y="N" ... font-size="N" fill="COLOR" opacity="N" ...>CHAR</text>
# Attributes may appear in any order; the long style= attribute is ignored for replacement.

TEXT_RE = re.compile(
    r'<text\s+'
    r'x="(?P<x>[^"]+)"\s+'
    r'y="(?P<y>[^"]+)"\s+'
    r'text-anchor="[^"]*"\s+'
    r'font-size="(?P<size>[^"]+)"\s+'
    r'fill="(?P<fill>[^"]+)"\s+'
    r'opacity="(?P<opacity>[^"]+)"'
    r'[^>]*>'                          # skip remaining attrs (style=...)
    r'(?P<char>[✦⬡])'
    r'</text>',
    re.DOTALL
)


def replace_match(m):
    replacement = text_to_shape(
        m.group("x"), m.group("y"), m.group("size"),
        m.group("fill"), m.group("opacity"), m.group("char")
    )
    if replacement is None:
        return m.group(0)   # unchanged
    return replacement


# ── Main ──────────────────────────────────────────────────────────────────────

shutil.copy(INPUT, BACKUP)
print(f"Backed up original to {BACKUP}")

with open(INPUT, "r", encoding="utf-8") as f:
    svg = f.read()

count_before = len(TEXT_RE.findall(svg))
fixed = TEXT_RE.sub(replace_match, svg)
count_after = len(TEXT_RE.findall(fixed))

with open(INPUT, "w", encoding="utf-8") as f:
    f.write(fixed)

print(f"Replaced {count_before - count_after} <text> elements")
print(f"Remaining <text> elements: {count_after}")
print("Done — arcane_sheet.svg updated in place.")
