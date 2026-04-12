# Keeper's Legacy - Complete Claude Code Handoff Package
## Step-by-Step Guide for Non-Coders

---

## TABLE OF CONTENTS

1. [What You're About to Do](#what-youre-about-to-do)
2. [Pre-Handoff Checklist](#pre-handoff-checklist)
3. [Step 1: Understand Claude Code](#step-1-understand-claude-code)
4. [Step 2: Prepare Design Files](#step-2-prepare-design-files)
5. [Step 3: Start Claude Code Project](#step-3-start-claude-code-project)
6. [Step 4: Upload Design Files](#step-4-upload-design-files)
7. [Step 5: Project Initialization](#step-5-project-initialization)
8. [Step 6: Development Workflow](#step-6-development-workflow)
9. [FAQ & Troubleshooting](#faq--troubleshooting)

---

## What You're About to Do

**In Plain English:**

You've designed a complete iOS game. Now you're going to hand that design to Claude Code (an AI assistant specialized in coding) who will:

1. **Read your design documents** (JSON files you created)
2. **Understand your vision** (story, mechanics, aesthetics, progression)
3. **Build the actual game code** (Swift, SwiftUI, SpriteKit)
4. **Ask clarifying questions** when needed
5. **Show you progress** as it builds

**You'll:**
- Review what Claude Code builds
- Provide feedback and direction
- Make decisions about features/changes
- Eventually approve a finished game ready for App Store

**Time to completion:** ~10 weeks of development (you work asynchronously - no daily meetings required)

---

## Pre-Handoff Checklist

Before you start, verify you have all 7 design files:

- [ ] **Keepers_Legacy_gdd.md** (Game Design Document)
- [ ] **creature_roster_complete.json** (58 creatures)
- [ ] **story_design_v1_2026-04-11.json** (Story & NPCs)
- [ ] **progression_system_v1_2026-04-11.json** (Levels & unlocks)
- [ ] **economy_system_v1_2026-04-12.json** (Currency & pricing)
- [ ] **technical_architecture_v1_2026-04-12.json** (Tech stack)
- [ ] **art_assets_v1_2026-04-12.json** (Visual design)

All these should be in `/mnt/user-data/outputs/`

**If any are missing:** Let me know, I'll regenerate them.

---

## Step 1: Understand Claude Code

### What is Claude Code?

Claude Code is a specialized version of Claude AI designed for software development. Think of it as:

- **A expert programmer** who understands your design
- **A translator** between your design docs and actual code
- **A builder** who writes Swift/SwiftUI for iOS games
- **Collaborative** - you work together, not hands-off

### How It Works

You'll have a **conversation with Claude Code** similar to what we've been doing, but focused on coding:

**You:** "Build the shop screen from the design"
**Claude Code:** Writes the Swift code, shows you progress
**You:** "Can you change the button color to match the art design?"
**Claude Code:** Updates code, explains changes

---

## Step 2: Prepare Design Files

### Download All Design Files

1. Go to `/mnt/user-data/outputs/` in this chat
2. Download each file:
   - `Keepers_Legacy_GDD.md`
   - `creature_roster_complete.json`
   - `story_design_v1_2026-04-11.json`
   - `progression_system_v1_2026-04-11.json`
   - `economy_system_v1_2026-04-12.json`
   - `technical_architecture_v1_2026-04-12.json`
   - `art_assets_v1_2026-04-12.json`

3. **Save them in a folder on your computer** (e.g., "Keeper's Legacy Design")

### Keep These Handy

During development, you'll reference these files. Keep them accessible on your computer.

---

## Step 3: Start Claude Code Project

### What is Claude Code? (Technical Detail)

Claude Code is accessed through a special "Code" interface that lets you:
- Create new files (Swift code, configuration, etc.)
- Edit files
- Run terminal commands
- Build iOS projects

### How to Access Claude Code

**Option A: Through Claude.ai (Easiest for Non-Coders)**

1. Go to **claude.ai**
2. **Start a new conversation**
3. Tell Claude: 
   ```
   "I'm starting a new iOS game project. 
   I have design documents ready. 
   Can you help me set up an Xcode project for Monster Pet Shop?"
   ```
4. Claude will guide you through setup
5. Use Claude's "Code" features to create/upload files

**Option B: Claude Code Direct (If Available)**

If you have access to Claude Code as a separate tool:
1. Launch Claude Code
2. Create new project: iOS Game - Monster Pet Shop
3. Choose Swift as language
4. Proceed to Step 4

### What Claude Code Will Ask

When you start, Claude Code will ask:
- "What's the project name?" → **Monster Pet Shop**
- "Which platform?" → **iOS**
- "Minimum iOS version?" → **iOS 15+** (from your tech design)
- "Which UI framework?" → **SwiftUI** (from your tech design)

Just refer to your technical architecture file for answers!

---

## Step 4: Upload Design Files

### Why Upload Files?

Claude Code needs to read your design files to understand:
- Creature data (names, descriptions, etc.)
- Economy rules (prices, coin values)
- Progression system (levels, unlocks)
- Visual specifications (colors, sizes)
- Story structure (acts, NPCs)

### How to Upload

**In Claude Code conversation:**

Tell Claude Code:
```
"I have 7 design JSON files ready. 
Can I upload them to the project so you can reference them while building?"
```

Claude Code will show you how to:
1. Drag-and-drop files into the project
2. Or paste file contents directly

**Upload these 7 files:**
1. `creature_roster_complete.json`
2. `story_design_v1.json`
3. `progression_system_v1.json`
4. `economy_system_v1.json`
5. `technical_architecture_v1.json`
6. `art_assets_v1.json`
7. `Monster_Pet_Shop_GDD.md` (also upload this)

### After Upload

Claude Code will:
- Read all design files
- Understand game structure
- Ask clarifying questions
- Start building project structure

---

## Step 5: Project Initialization

### What Claude Code Will Create

Once files are uploaded, Claude Code will create:

```
MonsterPetShop/
├── Sources/
│   ├── App/
│   │   └── MonsterPetShopApp.swift (main app file)
│   ├── Models/
│   │   ├── Creature.swift (from creature_roster JSON)
│   │   ├── Habitat.swift
│   │   ├── Economy.swift (from economy JSON)
│   │   ├── Progression.swift (from progression JSON)
│   │   └── Story.swift (from story JSON)
│   ├── ViewModels/
│   │   ├── ShopViewModel.swift
│   │   ├── HabitatViewModel.swift
│   │   └── CreatureViewModel.swift
│   ├── Views/
│   │   ├── ShopView.swift
│   │   ├── HabitatView.swift
│   │   ├── BreedingView.swift
│   │   └── SettingsView.swift
│   ├── Services/
│   │   ├── DataManager.swift (handles Core Data)
│   │   └── CloudSyncManager.swift (handles iCloud)
│   └── Resources/
│       └── GameConfig.swift (configuration from your specs)
├── Assets.xcassets/
│   └── (placeholders for creature sprites)
└── Project.pbxproj (Xcode configuration)
```

**Don't worry about this structure** - Claude Code handles it automatically.

### What This Means

- **Swift files** = actual code
- **GameConfig.swift** = your design constants (pricing, levels, etc.)
- **Models** = data structures matching your creatures/economy
- **Views** = UI screens (shop, habitats, etc.)

---

## Step 6: Development Workflow

### How Development Works

This is your day-to-day workflow with Claude Code:

### Week 1-2: Foundation

**You tell Claude Code:**
```
"Start with Task 2.1: Set up Xcode project structure 
and create data models from the design files."
```

**Claude Code:**
- Creates Swift data models
- Sets up project structure
- Creates basic app launch screen

**You:**
- Review what was built
- Provide feedback: "Looks good, proceed to next feature"
- Or: "Can you change X?"

### Week 3-5: Core Features

**You:** "Build the shop screen showing all creatures available for purchase"

**Claude Code:**
- Writes SwiftUI code for shop UI
- Implements purchase logic from economy design
- Creates creature display based on art asset specs
- Shows you a preview

**You:** "The colors should match the pastel palette from the art design. Can you adjust?"

**Claude Code:** Updates colors, shows preview

### Week 6-7: Systems

**You:** "Implement the breeding system with the costs specified in the economy design"

**Claude Code:**
- Adds breeding mechanics
- Implements rarity-based costs
- Creates breeding UI
- Integrates with creature system

### Week 8: Story Integration

**You:** "Add story progression based on the 3-act structure. Show NPCs and story text from the design."

**Claude Code:**
- Adds story system
- Creates NPC interactions
- Gates features based on story progress

### Week 9-10: Testing & Polish

**You:** "Run through the game. Test economy balance. Check progression pacing."

**Claude Code:**
- Helps identify bugs
- Optimizes performance
- Makes adjustments to balance

---

## How to Communicate with Claude Code

### Good Instructions

**✅ Specific and referencing design:**
```
"Build the Shop View. Show creatures in a grid with 3 columns. 
Use the creature data from creature_roster_complete.json. 
Show creature name, element icon, and price in Coins. 
Match the colors from art_assets_v1.json (soft greens, soft blues)."
```

**✅ Feature-focused:**
```
"Implement the Habitat View. Players should see their creatures in an isometric view.
Add buttons for: Feed, Play, Breed (unlocked at level 12).
When creatures are happy, show sparkle particles per the art design."
```

**✅ Referencing design requirements:**
```
"The progression should match progression_system_v1.json:
- 50 max level
- Slightly faster early game
- Milestone rewards at levels 5, 10, 25, 50
- Breeding unlocks at level 12-15 (after story milestone)"
```

### Things NOT to Say

**❌ Too vague:**
```
"Make it better"
"Add some features"
"Make the UI look good"
```

**❌ Too technical (if you don't know code):**
```
"Refactor the ViewModel architecture"
"Optimize the rendering pipeline"
```

Just describe what you want to see, and Claude Code will figure out the technical details!

---

## Development Milestones

### Milestone 1: Project Setup (Week 1)
- [ ] Xcode project created
- [ ] Data models built from design JSONs
- [ ] Core Data setup
- [ ] Basic app launch screen

### Milestone 2: Shop System (Week 2-3)
- [ ] Shop view showing creatures
- [ ] Purchase system working
- [ ] Creatures added to player collection
- [ ] Coins earned from sales

### Milestone 3: Habitat System (Week 3-4)
- [ ] Habitat view showing creatures
- [ ] Care mechanics (feed, play, care)
- [ ] Happiness system
- [ ] Visual feedback for creatures

### Milestone 4: Breeding System (Week 5-6)
- [ ] Breeding unlocks at level 12-15
- [ ] Breeding costs from economy design
- [ ] Mutation system (4 per species)
- [ ] New creatures born

### Milestone 5: Progression System (Week 6-7)
- [ ] Level system (1-50)
- [ ] Experience gain
- [ ] Milestone rewards
- [ ] Feature unlocks by level

### Milestone 6: Story Integration (Week 8)
- [ ] Story acts progress
- [ ] NPCs introduced
- [ ] Story-gated features
- [ ] Narrative progression

### Milestone 7: Polish & Testing (Week 9-10)
- [ ] Bug fixes
- [ ] Performance optimization
- [ ] Economy balance testing
- [ ] Progression pacing review
- [ ] Ready for App Store

---

## Making Changes During Development

### If You Want to Change Something

**Example: Breeding costs seem wrong**

**You tell Claude Code:**
```
"The rare creature breeding cost of 1000 coins 
seems too expensive based on player earn rate. 
Can we lower it to 500 coins? 
(Reference: economy_system_v1.json suggests variable costs)"
```

**Claude Code:**
- Updates the breeding cost
- Explains the change
- Tests the new balance

### Important: Version Your Changes

Keep track of changes:
```
DESIGN CHANGES LOG
- 2026-04-15: Lowered rare breeding cost from 1000 to 500 coins (economy balance)
- 2026-04-16: Adjusted purple color to match pastel theme (art assets)
- 2026-04-17: Story milestone moved from level 12 to level 10 (progression)
```

This helps you remember what was changed and why.

---

## FAQ & Troubleshooting

### Q: "Will I need to learn coding?"
**A:** No! Claude Code writes all the code. You just tell it what you want, and it builds it. Think of yourself as the Game Designer directing a programmer.

### Q: "How long will development take?"
**A:** ~10 weeks of development time. But since you work asynchronously, you can:
- Request features at your own pace
- Review progress whenever convenient
- It doesn't require daily commitment

### Q: "What if I don't like what Claude Code builds?"
**A:** You can always ask for changes:
- "Make the buttons bigger"
- "Use different colors"
- "Change the creature animation speed"
- "Reorganize the UI layout"

Claude Code will adjust and rebuild.

### Q: "What if Claude Code asks me questions?"
**A:** It will! Like:
- "Should the shop show creatures in a grid or list?"
- "How many creatures should be in each purchase pack?"
- "What should happen when breeding fails?"

Answer using your design documents. For example:
- "Check the shop interface design in art_assets_v1.json - it should be shelves/display"
- "Check economy_system_v1.json for pricing structure"

### Q: "Can I test the game while it's being built?"
**A:** Yes! Claude Code can:
- Create simulator builds
- Show you what the game looks like
- Let you test features as they're built

### Q: "What if I want to hire an actual iOS developer later?"
**A:** The code Claude Code writes is professional and portable:
- Standard Swift/SwiftUI
- Industry-best practices
- Well-structured and documented
- Easy for other developers to understand

### Q: "Will the game actually work on iPhones?"
**A:** Yes! Claude Code writes real, functional Swift code that:
- Builds in Xcode
- Runs on iOS devices (iPhone, iPad)
- Submits to App Store
- Works offline-first as designed

### Q: "What if something breaks or doesn't work?"
**A:** Claude Code will:
- Identify bugs
- Explain what went wrong
- Fix the issue
- Test the fix

You can also come back to me for design questions if needed.

---

## Next Steps After Development

### Once Game is Built (Week 10)

1. **App Store Submission**
   - Claude Code handles Xcode build settings
   - Create App Store Connect account (Apple)
   - Submit build for review (~24-48 hours)

2. **Soft Launch** (Optional)
   - Test on TestFlight (Apple's beta testing)
   - Get feedback from players
   - Make adjustments

3. **Launch**
   - Game goes live on App Store
   - Monitor performance
   - Gather player feedback

4. **Post-Launch Updates**
   - New creatures
   - New story content
   - Bug fixes
   - Improvements

---

## Your Design Documents Are Your Reference

Throughout development, your design files are the "source of truth":

- **Question: How many creatures?" → creature_roster_complete.json
- **Question: When does breeding unlock?** → progression_system_v1.json
- **Question: What colors should this be?** → art_assets_v1.json
- **Question: What's the story structure?** → story_design_v1.json
- **Question: What's the coin economy?** → economy_system_v1.json

Claude Code will reference these constantly.

---

## Summary: What Happens Now

### You Will:
1. ✅ Download all 7 design files
2. ✅ Start a Claude Code project
3. ✅ Upload the design files
4. ✅ Tell Claude Code: "Build based on these designs"
5. ✅ Review progress weekly/biweekly
6. ✅ Provide feedback and direction
7. ✅ In 10 weeks: Have a playable iOS game

### Claude Code Will:
1. ✅ Read all your design documents
2. ✅ Create project structure
3. ✅ Write Swift/SwiftUI code
4. ✅ Implement all mechanics per your specs
5. ✅ Build UI matching your art design
6. ✅ Test and optimize
7. ✅ Deliver a finished, working game

### The Result:
A fully functional iOS game ready for App Store, exactly matching your design vision.

---

## Are You Ready?

**Next Steps:**

1. **Download all 7 design files** from outputs folder
2. **Save them on your computer** in a folder called "Monster Pet Shop Design"
3. **Let me know when you're ready** to start Claude Code
4. **I'll give you the exact first prompt to use** with Claude Code

---

**Questions? Ask in the next message and I'll clarify before you start coding!**

**You're about to build a real iOS game. You've got this! 🚀**

---

*Generated: 2026-04-12*
*Design Phase: COMPLETE*
*Ready for Development: YES*
