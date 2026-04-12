# Creature Sprite Sheet Generation Prompt
# Copy everything between the ─── lines into Claude when requesting new sheets.

───────────────────────────────────────────────────────────────────────────────

Create a sprite sheet SVG for [CREATURE NAME] following these EXACT technical specifications. The sheet will be sliced programmatically by a game engine, so dimensions and layout must be pixel-perfect.

## CANVAS
- viewBox: exactly "0 0 512 512"
- width="512" height="512"
- No background rect on the root canvas
- No title text, no headers, no labels, no borders, no decorative elements of any kind

## GRID LAYOUT — 4 columns × 4 rows = 16 cells
- Each cell: exactly 128×128 pixels
- Cells are flush edge-to-edge — zero gap between cells, zero margin around the sheet
- Column 0 starts at x=0, Column 1 at x=128, Column 2 at x=256, Column 3 at x=384
- Row 0 starts at y=0, Row 1 at y=128, Row 2 at y=256, Row 3 at y=384

## COLUMN ORDER (mutations, left to right)
- Column 0: [MUTATION 0 NAME AND COLOR]
- Column 1: [MUTATION 1 NAME AND COLOR]
- Column 2: [MUTATION 2 NAME AND COLOR]
- Column 3: [MUTATION 3 NAME AND COLOR]

## ROW ORDER (animation states, top to bottom)
- Row 0 (y=0):   IDLE — creature at rest, calm posture, subtle weight
- Row 1 (y=128): INTERACT — creature reacting to player touch (mid-motion, engaged)
- Row 2 (y=256): HAPPY — visibly joyful (bounce, wide eyes, open expression)
- Row 3 (y=384): SAD — visibly unhappy (slumped, drooping, half-closed eyes)

## CELL BACKGROUNDS — TRANSPARENT
- Each 128×128 cell must have a TRANSPARENT background
- Do NOT add any rect or fill behind the creature
- The creature itself should have a natural background-free silhouette
- If SVG, use no background rect inside any cell region

## CREATURE STYLE
- Art style: Adventure Time-inspired — rounded shapes, bold outlines (2-3px), expressive large eyes
- Size within cell: creature occupies roughly 80-100px of the 128px cell (some breathing room at edges)
- Centered horizontally and vertically within its cell
- Consistent proportions across all 16 frames — same body size, only pose and color differ

## MUTATION RULES
- Same creature design, same body shape across all 4 columns
- Only colors change between mutations — no structural changes to the creature's shape
- Eyes should stay the same across mutations (dark pupils, white highlights)

## ANIMATION STATE RULES
- Same creature design across all 4 rows — only pose/expression changes
- IDLE: neutral standing pose, eyes open, relaxed
- INTERACT: slight lean forward, arms/limbs extended, excited eyes
- HAPPY: bouncy upward pose (translate the creature ~8px up), huge smile, sparkle optional
- SAD: slumped downward pose (translate ~8px down), small sad eyes, drooping features

## WHAT NOT TO INCLUDE
- No text of any kind
- No column or row labels
- No grid lines or cell borders
- No decorative frame or outer border
- No background panels or colored rectangles behind creatures
- No shadow beneath creatures
- No watermarks

## VERIFICATION CHECKLIST
Before finalizing, confirm:
☐ viewBox is exactly "0 0 512 512"
☐ 16 creature drawings present (4 cols × 4 rows)
☐ Cell at column 0, row 0 starts at exactly x=0, y=0
☐ Cell at column 3, row 3 ends at exactly x=512, y=512
☐ No background rects anywhere in the SVG
☐ No text nodes anywhere in the SVG
☐ Mutation 0 is in column 0 (leftmost)
☐ Idle state is in row 0 (topmost)

## CREATURE DESCRIPTION
[PASTE THE CREATURE'S DESCRIPTION FROM creature_roster_complete.json HERE]

Name: [NAME]
Habitat: [HABITAT TYPE]
Rarity: [RARITY]
Description: [DESCRIPTION]
Mutations:
  0: [COLOR NAME] — [describe the color scheme]
  1: [COLOR NAME] — [describe the color scheme]
  2: [COLOR NAME] — [describe the color scheme]
  3: [COLOR NAME] — [describe the color scheme]

───────────────────────────────────────────────────────────────────────────────

## HOW TO USE THIS PROMPT

1. Copy everything between the ─── lines
2. Replace all [BRACKETED] fields with the creature's actual data
3. Paste into Claude and send
4. Save the SVG output as:  {creatureID}_sheet.svg  (e.g. aquaburst_sheet.svg)
   Note: NO "_sprite_sheet" suffix — just "_sheet"
5. Convert to PNG using convert_to_png.bat (Inkscape required)
6. Add to Xcode: Assets.xcassets/Creatures/{creatureID}_sheet.imageset/

## EXAMPLE FILLED-IN (Aquaburst)

Replace the [BRACKETED] sections with:

- MUTATION 0: Tidal Blue — deep ocean blue body, pale blue belly
- MUTATION 1: Pearl White — soft white/cream body, pale green tint
- MUTATION 2: Abyssal Dark — deep navy/indigo body, dark teal fins
- MUTATION 3: Coral Rose — warm pink/coral body, salmon fins

Creature Description:
  Name: Aquaburst
  Habitat: Water
  Rarity: Common
  Description: A bubbly water sprite that leaves sparkling trails wherever it swims.
