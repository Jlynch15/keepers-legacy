# Keeper's Legacy - Detailed Game Design Document

**Version:** 1.0  
**Last Updated:** April 2026  
**Status:** LOCKED - Ready for Development

---

## Executive Summary

Keeper's Legacy is a modern reimagining of the classic 2011 iOS pet care simulator. Players manage a magical creature shop, caring for and breeding diverse monsters to complete their collection while uncovering the story behind their shop's origins. The game emphasizes relaxation, discovery, and long-term engagement through a hybrid of instant-gratification mechanics and meaningful waiting periods.

**Target Platforms:** iOS 15+ (iPhone & iPad)  
**Business Model:** Free-to-play with cosmetic-only IAP, no ads, no pay-to-win  
**Development Timeline:** ~18 weeks to launch

---

## 1. CORE VISION & PILLARS

### What Makes This Game Special
- **Enhanced Nostalgia:** Respects the original's charm while modernizing the experience
- **Story-Driven:** A narrative unfolds as players progress, explaining the shop's origins
- **Flexible Engagement:** Players choose their own pace - quick check-ins or extended sessions
- **Magical Aesthetic:** Beautiful hand-drawn art with whimsical magical elements
- **Fair Monetization:** Cosmetics only, never blocking gameplay progression

### Design Pillars
1. **Relaxing & Stress-Free** - No pressure, no timers forcing action
2. **Rewarding Collection** - Discovery-driven progression
3. **Magical & Beautiful** - Whimsical art direction throughout
4. **Story & Discovery** - Narrative reveals gameplay systems
5. **Player Agency** - Choose when and how you engage

---

## 2. GAMEPLAY OVERVIEW

### Core Loop (Daily/Weekly Engagement)

```
Check In → Care for Monsters → Play with Creatures → 
Breeding/Collection → Story Progression → Customize Shop → Repeat
```

### Session Structure

**Flexible Session Length:** Players decide their own pace
- **Quick Session (5 min):** Check on creatures, feed, quick interaction
- **Medium Session (15 min):** Full care routine, check on eggs, manage inventory
- **Extended Session (30+ min):** Breeding planning, collection hunting, story exploration

Players are NOT pressured into any session length.

### The Shop

- Players inherit/discover a magical creature shop
- Start with 1-2 small habitats
- Expand to 5+ habitats as they progress
- Each habitat holds 1 monster at a time
- Habitats are categorized: Water, Dirt, Grass, Magical (unlocked later)
- Shop can be customized with decorations (cosmetic IAP)

---

## 3. MONSTER SYSTEM

### Monster Count & Variety
- **25+ Monster Species** (core content)
- **4 Mutations per Species** = 100+ unique looks to collect
- **Monsters scale with progression** - some unlocked early, others require breeding/story progress

### Monster Attributes & Care Needs

Each monster has 5 core stats that fluctuate:

| Stat | Description | How to Improve |
|------|-------------|----------------|
| **Hunger** | 0-100% (starts empty) | Feed with appropriate food |
| **Happiness** | 0-100% (affects joy) | Play with preferred toys |
| **Cleanliness** | 0-100% (gets dirty over time) | Clean habitat regularly |
| **Affection** | 0-100% (bond with player) | Pet in favorite spots |
| **Playfulness** | 0-100% (energy level) | Varies by species, recharged by play |

### Monster Lifecycle

1. **Egg Stage** (0-12 hours real-time, optional waiting)
   - Player must choose to incubate
   - Can skip and sell egg to adoption agency for currency
   - Incubation time varies by species (rare = longer)

2. **Baby Stage** (1-2 in-game days)
   - Requires frequent feeding and attention
   - High stat decay
   - Can't breed yet
   - Stats must be kept above 20% or monster becomes sad

3. **Adolescent Stage** (3-5 in-game days)
   - Stats stabilize, less frequent care needed
   - Can start playing with toys
   - Still can't breed
   - Develops personality traits

4. **Adult Stage** (permanent)
   - Fully mature, all features unlocked
   - Can breed with other adults
   - Stats decay slower, easier to maintain
   - Sells for best price to customers

### Breeding System

**How Breeding Works:**
1. Select two adult monsters of compatible types
2. Pay breeding fee (soft currency)
3. Egg appears in incubation room within 1 hour (instant or optional wait)
4. Player chooses to incubate or abandon egg
5. Offspring has traits influenced by parents (50/50 genetic inheritance)
6. Mutations can only be obtained through breeding (incentivizes collection)

**Genetic Inheritance:**
- Each monster has 4 "genes" determining appearance
- Offspring inherits 2 genes from each parent
- Rare/special traits have lower inheritance probability
- Encourages strategic breeding for rare mutations

**Breeding Rules:**
- Same species only (no cross-breeding)
- Both parents must be adults
- Parents not removed from habitat when breeding
- Can breed same parents multiple times
- Some special "legendary" monsters require specific parent combinations

### Monster Preferences & Discovery

**The discovery mechanic from the original is KEPT:**
- Each monster species has favorite toys
- Game does NOT tell you upfront
- Players must try different toys (trial and error)
- Customers sometimes hint at preferences
- Successful play = happiness boost + currency

**Example:**
- Flamekit likes Heat Lamps and Red Toys
- Aquari likes Musical Instruments and Water Effects
- Frostwhisper likes Cold/Ice themed toys

---

## 4. STORY & NARRATIVE PROGRESSION

### Narrative Framework

**Act I: Discovery (Early Game)**
- Player inherits a mysterious shop from absent uncle
- Shop is in disrepair, few monsters
- Letters/journal entries reveal uncle's past
- Player discovers the shop has magical origins
- Core mechanics introduced through story events

**Act II: Restoration (Mid Game)**
- Unlocks new habitat types and advanced mechanics
- Discover the shop's history through monster interactions
- Build relationships with recurring NPCs
- Learn about the magical ecosystem

**Act III: Legacy (Late Game)**
- Uncover the full mystery of the shop's origins
- Make choices about the shop's future
- Unlock special "legacy" monsters
- End-game content becomes available
- Hints at expanded story post-launch

### Story Delivery Methods

1. **Welcome Letters** - Appear in shop, advance narrative
2. **NPC Interactions** - Regular customers who develop relationships
3. **Monster Discoveries** - New species come with lore snippets
4. **Environmental Details** - Shop decorations reveal history
5. **Optional Journal** - Detailed story for players who want it, skippable for others

### Progression Gates

Certain monsters and features are locked behind story milestones:
- Habitat 2 unlocked after Act I
- Magical habitat unlocked in Act II
- Breeding unlocked at specific story point
- Monsterpedia full features unlock through progression

---

## 5. GAMEPLAY MECHANICS

### Care System (Interactive)

**Feeding**
- Tap food item on monster
- Monster animates eating
- Hunger stat decreases
- Different foods preferred by different species
- Higher-quality foods fill hunger more efficiently

**Petting/Affection**
- Tap on monster's favorite spots (head, belly, back)
- Visual feedback (sparkles, happy animation)
- Affection stat increases
- Different species have different favorite spots
- Unlocked as story progression reveals preferences

**Playing**
- Select toy from inventory
- Monster plays if it matches preference
- If preference unknown, monster might ignore or play halfheartedly
- Happiness increases, playfulness decreases
- Successful play with preferred toy = bonus currency

**Cleaning**
- Habitat accumulates dirt over time (visual indicator on screen)
- Select cleaning tool
- Mini-game: drag/swipe to clean dirty areas
- Cleanliness stat increases
- Cheaper tools break faster (cost vs. durability tradeoff)
- Can hire automatic cleaner (cosmetic IAP option)

### Breeding Mechanics

**Egg Production:**
1. Visit breeding chamber
2. Select two adult monsters
3. Confirm breeding (costs soft currency)
4. Egg appears in incubator after short delay
5. Player chooses to incubate (real-time wait) or abandon

**Incubation:**
- Incubation time: 2-12 hours real-time (varies by species)
- Player can speed-up with soft currency (medium cost)
- Incubation progress shown visually
- Egg hatches automatically, requires empty habitat or monster moved to adoption

**Post-Hatch:**
- Baby appears in habitat
- Different appearance/color based on genetics
- Immediately requires care
- Player discovers if rare mutation obtained (exciting moment!)

### Collection & Monsterpedia

**Monsterpedia Features:**
- Track all discovered species (25+)
- Track all mutations per species (4 each)
- View detailed stats and preferences for each
- See breeding history and lineage
- Unlock entries by discovering monsters

**Collection Challenges:**
- Complete species collection = reward
- Complete all mutations of a species = special reward
- Certain rare combinations unlock achievements

**Discovery Progression:**
- Early game: Mostly common species available
- Mid game: New species introduced via breeding
- Late game: Rare/legendary combinations available
- Post-game: Special event-only monsters (future content)

### Economy System

**Soft Currency: Monster Coins (earned in-game)**
- Earned by: selling monsters to customers, completing quests, achievements
- Spend on: food, toys, tools, habitats, breeding, speed-ups
- Players can progress indefinitely without spending real money
- Balanced so steady players always have sufficient currency

**Premium Currency: Stardust (purchased with real money, cosmetic only)**
- Spend on: cosmetic shop themes, creature cosmetics, habitat decorations
- Never needed for progression
- Starter pack option (optional)
- No aggressive push to purchase

**Pricing Philosophy:**
- Cosmetics make the experience prettier, not gameplay faster
- Core players never feel disadvantaged
- Monetization supports development, never exploits players

### Customer System

Customers visit the shop requesting specific monsters:
- "I'd like a happy adult Flamekit"
- "Can you breed me a rare mutation Aquari?"
- Player fulfills order = payment + customer satisfaction
- Regular customers build relationships
- Reputation/happiness affects future orders

---

## 6. PROGRESSION SYSTEM

### Player Leveling

**Experience Earned By:**
- Caring for monsters (feeding, petting, cleaning)
- Completing customer orders
- Breeding monsters
- Discovering new species
- Story milestones

**Level Rewards (Every 5 levels):**
- Habitat slot expansion
- New decorations/cosmetics
- Soft currency bonus
- Story progression gates
- Feature unlocks (breeding, new habitat types, etc.)

### Progression Timeline

**Early Game (Levels 1-10, ~1-2 weeks)**
- Learn basic mechanics
- Unlock breeding
- Complete Act I story
- Have 2-3 habitats

**Mid Game (Levels 11-25, ~2-4 weeks)**
- Hunt for mutations
- Unlock Magical habitat
- Complete Act II story
- Active breeding strategies

**Late Game (Levels 26+, ~4+ weeks)**
- Complete species collection
- Hunt rare mutations
- Unlock Act III content
- Cosmetic focus and shop customization

### Pacing
- Not a race - players progress at own pace
- No daily login requirements to stay competitive
- Catch-up mechanics for lapsed players
- Multiple content paths (collecting, breeding, story, cosmetics)

---

## 7. ART DIRECTION

### Visual Style
- **Primary Style:** Modern Hand-Drawn Illustration
- **Reference:** Adventure Time aesthetic - soft, colorful, expressive
- **Evolution:** Build from original Monster Pet Shop into contemporary style
- **Goal:** Recognize original monsters, but in modern artistic form

### Art Specifications

**Monster Art:**
- Hand-drawn 2D sprites
- Expressive animation frames (idle, interact, happy, sad, eating, sleeping)
- Consistent character design language
- 25+ species with 4 mutations each = 100+ unique designs
- Color palettes match habitat types (water/blue, dirt/brown, grass/green)
- "Magical" variants get sparkle/glow effects

**UI Theme: Magical/Whimsical**
- Sparkle effects, crystal motifs, dreamy color palette
- Shop interface has magical shop aesthetic
- Buttons decorated with crystalline/mystical elements
- Shop backgrounds emphasize fantasy/magical setting
- Smooth, rounded UI components
- Soft color palette (pastels, iridescent accents)

**Animation Focus:**
- Monsters are expressive and emotive
- Reaction animations for every interaction
- Smooth transitions and visual feedback
- Particle effects (sparkles, hearts, etc.)
- Screen shake/bounce for important moments
- No jarring transitions - fluid, magical feel

**Audio Aesthetics:**
- Soft, whimsical music (not intense)
- Magical sound effects (chimes, sparkles)
- Creature vocalizations (chirps, growls, etc.)
- Ambient shop sounds (subtle magic hum)
- All audio supports relaxing experience

### Color Palette
- Primary: Soft pastels (lavenders, soft blues, mint greens)
- Accents: Gold, silver, iridescent shimmer
- Habitats: Water (blues), Dirt (browns), Grass (greens), Magical (purples/pinks)
- UI: Cream/soft whites with sparkle overlays

---

## 8. TECHNICAL SPECIFICATIONS

### Platform & Support
- **Platform:** iOS only (for now)
- **Minimum iOS:** 15.0+
- **Supported Devices:** iPhone and iPad (universal app)
- **Target Devices:**
  - iPhone SE, iPhone 12 mini and newer (primary)
  - Standard iPhones (12, 13, 14, 15 series)
  - iPad (all sizes, landscape support)
- **Aspect Ratios:** 9:16 to 16:9 (responsive design)

### Technical Architecture
- **Language:** Swift (native iOS)
- **UI Framework:** SwiftUI (modern, declarative)
- **Graphics:** SpriteKit (2D rendering)
- **Data:** Core Data (local persistence)
- **Architecture Pattern:** MVVM (clean separation)
- **Target Performance:** 60 FPS, <100MB app size
- **Memory Target:** <150MB at peak usage

### Connectivity Model
- **Primary:** Offline-first gameplay
- **Optional Feature:** iCloud save backup (optional sync)
- **No Required Online:** Core gameplay never requires internet
- **Future-Ready:** Architecture supports multiplayer/trading in future updates

---

## 9. MONETIZATION MODEL

### Free-to-Play with Optional IAP

**Revenue Streams:**
- Cosmetic In-App Purchases (ONLY)
- NO ads
- NO pay-to-win mechanics
- NO content gating behind paywalls

**What Players Can Buy (Cosmetics Only):**

1. **Shop Themes** ($2.99-$4.99)
   - Alternative visual styles for the shop
   - Does not affect gameplay
   - Purely aesthetic

2. **Monster Cosmetics** ($0.99-$1.99)
   - Clothing items for monsters
   - Accessories/hats
   - Does not affect stats or care needs

3. **Habitat Decorations** ($1.99-$3.99)
   - Furniture, plants, structures
   - Visual customization only
   - No gameplay impact

4. **Starter Pack** ($4.99, Optional)
   - Early-game currency boost
   - Convenience for first-time players
   - No advantage over free players (catch-up only)

**What Players CANNOT Buy:**
- ❌ Faster care requirements
- ❌ Additional habitats (earned through leveling)
- ❌ Better breeding rates
- ❌ Rare mutations or species
- ❌ Artificial progression shortcuts
- ❌ Any gameplay advantage

### Pricing Philosophy

**Balance Between:**
- Supporting development costs
- Never forcing players to spend
- Making cosmetics genuinely appealing

**Target:**
- 2-3% of players purchase cosmetics
- Average revenue per user: $0.50-$2.00
- Sustainable for ongoing development

---

## 10. FEATURES SUMMARY

### Core Features (Launch)
✅ Monster care system (feed, pet, clean, play)
✅ Breeding with genetics
✅ Shop management (habitats, expansion)
✅ 25+ species with 4 mutations each
✅ Story-driven progression (3 acts)
✅ Customer order system
✅ Monsterpedia collection tracker
✅ Cosmetic shop customization
✅ Offline gameplay with optional cloud save
✅ Hand-drawn beautiful art style
✅ Magical whimsical UI

### Features Not in Launch (Post-Release)
🔄 Multiplayer trading (future)
🔄 Seasonal events
🔄 New monster species
🔄 New story acts
🔄 Competitive features (leaderboards)
🔄 Android version

---

## 11. SUCCESS METRICS

### Player Engagement
- Daily active users (DAU)
- Session length (flexible, measured not required)
- Monthly active users (MAU)
- Retention at 1-week, 1-month, 3-month

### Progression
- Average player level reached
- Collection completion rate
- Breeding activity rate
- Story completion rate

### Monetization
- Install-to-customer conversion (target: 2-3%)
- Average revenue per user (ARPU)
- Customer lifetime value

### Quality
- Crash rate (target: <0.1%)
- User reviews (target: 4.5+ stars)
- Session stability

---

## 12. GAME DESIGN DOCUMENT SIGN-OFF

| Item | Decision |
|------|----------|
| **Gameplay Style** | Enhanced remake with modern improvements |
| **Session Length** | Flexible - player decides their pace |
| **Waiting Mechanics** | Hybrid (some instant, some require waiting) |
| **Story** | Story-driven progression (3 acts) |
| **Art Style** | Modern hand-drawn illustration (Adventure Time-style) |
| **UI Theme** | Magical/whimsical with sparkles & crystals |
| **Target Audience** | Broad appeal (all ages, families, casual players) |
| **Animation Level** | Expressive, reactive animations |
| **Platform** | iOS 15+ (iPhone & iPad) |
| **Internet Model** | Offline-first, optional cloud save |
| **Business Model** | Free-to-play, cosmetic-only IAP, no ads |
| **Monetization Goal** | Balanced - support development without aggression |
| **Pay-to-Win Tolerance** | ZERO - never required to pay for gameplay |
| **Ad Acceptance** | No ads at all |

---

## 13. WHAT'S NEXT?

This GDD locks in all major creative decisions. We now move to:

1. **Detailed Monster Design Document** - Every species, their mutations, stats
2. **Narrative/Story Outline** - Full 3-act story with script
3. **Technical Architecture Document** - Code structure, data models
4. **Art Asset List** - Everything that needs to be created
5. **Development Task Breakdown** - Specific coding tasks
6. **Content Production Schedule** - Timeline for art, audio, etc.

Each of these becomes the foundation for actual development work.

---

**Document Version:** 1.0  
**Last Updated:** April 2026  
**Status:** ✅ LOCKED - Ready for detailed design phase  
**Next Review:** After technical architecture document completion
