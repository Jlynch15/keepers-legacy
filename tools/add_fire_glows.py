"""Add creature-specific glow shapes to each fire SVG, inserted right after </style>."""
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
FIRE = ROOT / "KeeperLegacyGodot" / "Sprites" / "Creatures" / "fire"

# Each creature gets a custom glow block tailored to its signature feature.
# The block is inserted immediately after </style>.
GLOWS = {
    "cinderborne": """
  <!-- Custom glow: flickering warmth concentrated around the crown -->
  <ellipse cx="256" cy="186" rx="100" ry="50" fill="#ff8038" opacity="0.18"/>
  <ellipse cx="256" cy="186" rx="68" ry="34" fill="#ffc080" opacity="0.25"/>
""",
    "scorchwhirl": """
  <!-- Custom glow: motion-blur trail along the spiral path -->
  <path d="M256,156 Q396,200 380,340 Q344,440 224,432 Q116,408 124,294" stroke="#ff8020" stroke-width="40" fill="none" stroke-linecap="round" opacity="0.18"/>
""",
    "flamewing": """
  <!-- Custom glow: soft red glow behind each wing -->
  <ellipse cx="100" cy="298" rx="100" ry="80" fill="#ff5848" opacity="0.18"/>
  <ellipse cx="412" cy="298" rx="100" ry="80" fill="#ff5848" opacity="0.18"/>
""",
    "volatile": """
  <!-- Custom glow: irregular bursting glow at each flame tip -->
  <circle cx="256" cy="120" r="32" fill="#ff7848" opacity="0.25"/>
  <circle cx="368" cy="180" r="28" fill="#ff7848" opacity="0.22"/>
  <circle cx="400" cy="296" r="32" fill="#ff7848" opacity="0.25"/>
  <circle cx="368" cy="412" r="28" fill="#ff7848" opacity="0.22"/>
  <circle cx="256" cy="448" r="32" fill="#ff7848" opacity="0.22"/>
  <circle cx="144" cy="412" r="28" fill="#ff7848" opacity="0.22"/>
  <circle cx="112" cy="296" r="32" fill="#ff7848" opacity="0.25"/>
  <circle cx="144" cy="180" r="28" fill="#ff7848" opacity="0.22"/>
""",
    "emberpaw": """
  <!-- Custom glow: warm glow under each foot — paw print effect -->
  <ellipse cx="200" cy="446" rx="48" ry="20" fill="#ffd060" opacity="0.32"/>
  <ellipse cx="312" cy="446" rx="48" ry="20" fill="#ffd060" opacity="0.32"/>
""",
    "sparksnout": """
  <!-- Custom glow: bright glow around the snout (the crackling source) -->
  <circle cx="256" cy="304" r="120" fill="#ffd040" opacity="0.2"/>
  <circle cx="256" cy="304" r="92" fill="#fff088" opacity="0.18"/>
""",
    "magmakin": """
  <!-- Custom glow: heat-shimmer rising from molten cracks -->
  <ellipse cx="256" cy="266" rx="160" ry="22" fill="#ff5020" opacity="0.18"/>
  <ellipse cx="256" cy="346" rx="100" ry="18" fill="#ff5020" opacity="0.18"/>
  <ellipse cx="256" cy="394" rx="80" ry="16" fill="#ff5020" opacity="0.15"/>
""",
    "ashwalker": """
  <!-- Custom glow: subtle warm glow at base only (calm ash drift) -->
  <ellipse cx="256" cy="446" rx="160" ry="32" fill="#ff8038" opacity="0.18"/>
""",
    # solarflare's sun rays + halo are already the glow — add minimal subtle warmth
    "solarflare": """
  <!-- Custom glow: bright radiance around the central body -->
  <circle cx="256" cy="306" r="148" fill="#ffe888" opacity="0.18"/>
  <circle cx="256" cy="306" r="124" fill="#fff088" opacity="0.22"/>
""",
    "infernokin": """
  <!-- Custom glow: scattered cosmic star-points (constellation, not a halo) -->
  <circle cx="60"  cy="120" r="2" fill="#ffffff" opacity="0.85"/>
  <circle cx="120" cy="60"  r="2" fill="#ffffff" opacity="0.8"/>
  <circle cx="450" cy="80"  r="2" fill="#ffffff" opacity="0.85"/>
  <circle cx="490" cy="180" r="2" fill="#ffffff" opacity="0.8"/>
  <circle cx="20"  cy="320" r="2" fill="#ffffff" opacity="0.8"/>
  <circle cx="488" cy="380" r="2" fill="#ffffff" opacity="0.85"/>
  <circle cx="40"  cy="448" r="2" fill="#ffffff" opacity="0.8"/>
  <circle cx="180" cy="40"  r="2" fill="#ffffff" opacity="0.75"/>
  <circle cx="380" cy="468" r="2" fill="#ffffff" opacity="0.8"/>
  <!-- soft cosmic violet aura right behind body only -->
  <ellipse cx="256" cy="306" rx="140" ry="160" fill="#ff4818" opacity="0.15"/>
""",
}


def main() -> None:
    for creature_id, glow_block in GLOWS.items():
        svg = FIRE / creature_id / f"{creature_id}.svg"
        text = svg.read_text(encoding="utf-8")
        if "Custom glow:" in text:
            print(f"  SKIP {creature_id} — already has custom glow")
            continue
        # Insert glow block after </style>
        new_text = text.replace("</style>", "</style>" + glow_block, 1)
        svg.write_text(new_text, encoding="utf-8")
        print(f"  added custom glow to fire/{creature_id}")


if __name__ == "__main__":
    main()
