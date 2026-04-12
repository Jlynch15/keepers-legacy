import Foundation

// MARK: - Complete Creature Catalog
// Sourced from creature_roster_complete.json
// 58 base creatures across 7 habitat types

extension CreatureCatalogEntry {

    static let allCreatures: [CreatureCatalogEntry] = waterCreatures
        + dirtCreatures
        + grassCreatures
        + fireCreatures
        + iceCreatures
        + electricCreatures
        + magicalCreatures

    // MARK: - Water (15)

    static let waterCreatures: [CreatureCatalogEntry] = [
        CreatureCatalogEntry(
            id: "aquaburst",
            name: "Aquaburst",
            description: "A bubbly water sprite that leaves sparkling trails wherever it swims.",
            habitatType: .water, rarity: .common, favoriteToy: "Bubble Wand",
            mutations: defaultMutations(colors: ["Sapphire Blue", "Teal Green", "Pearl White", "Coral Pink"])
        ),
        CreatureCatalogEntry(
            id: "shimmerstream",
            name: "Shimmerstream",
            description: "Its scales catch light like a prism, casting rainbows across the habitat.",
            habitatType: .water, rarity: .common, favoriteToy: "Shiny Pebble",
            mutations: defaultMutations(colors: ["Rainbow Silver", "Deep Blue", "Rose Gold", "Midnight"])
        ),
        CreatureCatalogEntry(
            id: "seraphine",
            name: "Seraphine",
            description: "A graceful creature with flowing fins that resemble angel wings.",
            habitatType: .water, rarity: .uncommon, favoriteToy: "Silk Ribbon",
            mutations: defaultMutations(colors: ["Celestial White", "Ocean Blue", "Lavender", "Gold"])
        ),
        CreatureCatalogEntry(
            id: "deepecho",
            name: "Deepecho",
            description: "Born in the darkest ocean trenches, it communicates through haunting melodies.",
            habitatType: .water, rarity: .uncommon, favoriteToy: "Musical Shell",
            mutations: defaultMutations(colors: ["Abyss Black", "Bioluminescent Blue", "Deep Purple", "Emerald"])
        ),
        CreatureCatalogEntry(
            id: "tidecaller",
            name: "Tidecaller",
            description: "Can predict weather changes hours before they arrive.",
            habitatType: .water, rarity: .common, favoriteToy: "Driftwood",
            mutations: defaultMutations(colors: ["Storm Grey", "Seafoam", "Sandy Tan", "Slate Blue"])
        ),
        CreatureCatalogEntry(
            id: "coralsprite",
            name: "Coralsprite",
            description: "Lives among coral reefs and can grow tiny coral formations from its fingertips.",
            habitatType: .water, rarity: .common, favoriteToy: "Coral Piece",
            mutations: defaultMutations(colors: ["Vivid Orange", "Pink Coral", "Pale Yellow", "Purple"])
        ),
        CreatureCatalogEntry(
            id: "mistwalker",
            name: "Mistwalker",
            description: "Glides across the water's surface, leaving a trail of morning mist.",
            habitatType: .water, rarity: .uncommon, favoriteToy: "Fog Machine",
            mutations: defaultMutations(colors: ["Misty White", "Grey Blue", "Pale Green", "Silver"])
        ),
        CreatureCatalogEntry(
            id: "wavecrest",
            name: "Wavecrest",
            description: "Rides ocean waves with extraordinary skill and loves performing for crowds.",
            habitatType: .water, rarity: .common, favoriteToy: "Surfboard Toy",
            mutations: defaultMutations(colors: ["Ocean Blue", "Foam White", "Sunset Orange", "Turquoise"])
        ),
        CreatureCatalogEntry(
            id: "pearlescent",
            name: "Pearlescent",
            description: "Its body glows with an inner pearlescent light that intensifies with happiness.",
            habitatType: .water, rarity: .rare, favoriteToy: "Pearl Ball",
            mutations: defaultMutations(colors: ["Pure White", "Blush Pink", "Champagne", "Iridescent"])
        ),
        CreatureCatalogEntry(
            id: "riptide",
            name: "Riptide",
            description: "Fast and forceful, can generate powerful currents with a flick of its tail.",
            habitatType: .water, rarity: .uncommon, favoriteToy: "Speed Spinner",
            mutations: defaultMutations(colors: ["Electric Blue", "Navy", "Bright Teal", "Storm White"])
        ),
        CreatureCatalogEntry(
            id: "bubblesnout",
            name: "Bubblesnout",
            description: "Has an oversized snout that shoots bubbles in intricate patterns when excited.",
            habitatType: .water, rarity: .common, favoriteToy: "Bubble Tube",
            mutations: defaultMutations(colors: ["Sky Blue", "Soft Pink", "Lime Green", "Pale Yellow"])
        ),
        CreatureCatalogEntry(
            id: "kelpling",
            name: "Kelpling",
            description: "Wears a cape of living kelp that sways even when perfectly still.",
            habitatType: .water, rarity: .common, favoriteToy: "Kelp Strand",
            mutations: defaultMutations(colors: ["Forest Green", "Dark Blue", "Brown", "Golden Yellow"])
        ),
        CreatureCatalogEntry(
            id: "frostfin",
            name: "Frostfin",
            description: "A cold-water denizen whose fins are tipped with permanent ice crystals.",
            habitatType: .water, rarity: .uncommon, favoriteToy: "Ice Cube",
            mutations: defaultMutations(colors: ["Ice Blue", "Glacier White", "Pale Purple", "Crystal Clear"])
        ),
        CreatureCatalogEntry(
            id: "swirlpool",
            name: "Swirlpool",
            description: "Creates miniature whirlpools for fun. Its body naturally spirals when resting.",
            habitatType: .water, rarity: .common, favoriteToy: "Spinning Top",
            mutations: defaultMutations(colors: ["Turquoise", "Deep Blue", "Teal", "Aquamarine"])
        ),
        CreatureCatalogEntry(
            id: "luminara",
            name: "Luminara",
            description: "A rare deep-sea creature whose bioluminescence can light an entire room.",
            habitatType: .water, rarity: .rare, favoriteToy: "Glow Stick",
            mutations: defaultMutations(colors: ["Electric Blue", "Neon Green", "Vivid Pink", "Golden White"])
        ),
    ]

    // MARK: - Dirt (15)

    static let dirtCreatures: [CreatureCatalogEntry] = [
        CreatureCatalogEntry(
            id: "crumblebane",
            name: "Crumblebane",
            description: "Constantly shedding small pebbles it collects obsessively from the earth.",
            habitatType: .dirt, rarity: .common, favoriteToy: "Pebble Collection",
            mutations: defaultMutations(colors: ["Sandstone", "Dark Brown", "Red Clay", "Ash Grey"])
        ),
        CreatureCatalogEntry(
            id: "dustdevil",
            name: "Dustdevil",
            description: "Whips up tiny dust tornadoes when spinning in excitement.",
            habitatType: .dirt, rarity: .common, favoriteToy: "Pinwheel",
            mutations: defaultMutations(colors: ["Dust Brown", "Tan", "Russet", "Pale Beige"])
        ),
        CreatureCatalogEntry(
            id: "bedrock",
            name: "Bedrock",
            description: "The sturdiest of all dirt creatures — nothing can knock it over.",
            habitatType: .dirt, rarity: .uncommon, favoriteToy: "Boulder Toy",
            mutations: defaultMutations(colors: ["Granite Grey", "Dark Brown", "Iron Black", "Sandy Tan"])
        ),
        CreatureCatalogEntry(
            id: "stonework",
            name: "Stonework",
            description: "Carves intricate patterns into nearby rocks using its hardened claws.",
            habitatType: .dirt, rarity: .uncommon, favoriteToy: "Carving Tool",
            mutations: defaultMutations(colors: ["Marble White", "Obsidian", "Sandstone", "Moss Green"])
        ),
        CreatureCatalogEntry(
            id: "mudbubble",
            name: "Mudbubble",
            description: "Absolutely loves mud baths. The muddier, the happier.",
            habitatType: .dirt, rarity: .common, favoriteToy: "Mud Pie",
            mutations: defaultMutations(colors: ["Chocolate Brown", "Clay Red", "Grey Mud", "Pale Yellow"])
        ),
        CreatureCatalogEntry(
            id: "terraclaw",
            name: "Terraclaw",
            description: "Its oversized claws are perfect for digging elaborate tunnel networks.",
            habitatType: .dirt, rarity: .common, favoriteToy: "Shovel",
            mutations: defaultMutations(colors: ["Earth Brown", "Orange Clay", "Dark Grey", "Sand"])
        ),
        CreatureCatalogEntry(
            id: "sandwhistle",
            name: "Sandwhistle",
            description: "Produces a haunting whistle by blowing air through porous sand-filled cavities.",
            habitatType: .dirt, rarity: .uncommon, favoriteToy: "Wind Chime",
            mutations: defaultMutations(colors: ["Desert Sand", "Pale Orange", "Tan", "Dune Gold"])
        ),
        CreatureCatalogEntry(
            id: "rootbound",
            name: "Rootbound",
            description: "Has roots growing from its feet that anchor it during sleep.",
            habitatType: .dirt, rarity: .common, favoriteToy: "Seed Packet",
            mutations: defaultMutations(colors: ["Brown", "Dark Root", "Pale Wood", "Mossy Green"])
        ),
        CreatureCatalogEntry(
            id: "gravelgrip",
            name: "Gravelgrip",
            description: "Hands permanently coated in a layer of gravel it uses as natural armor.",
            habitatType: .dirt, rarity: .common, favoriteToy: "Rock Tumbler",
            mutations: defaultMutations(colors: ["Gravel Grey", "Slate", "Brown", "Black Pebble"])
        ),
        CreatureCatalogEntry(
            id: "claymold",
            name: "Claymold",
            description: "Its body is malleable — it can reshape itself into rough sculptures.",
            habitatType: .dirt, rarity: .uncommon, favoriteToy: "Clay Block",
            mutations: defaultMutations(colors: ["Red Clay", "Grey", "Terracotta", "Dark Brown"])
        ),
        CreatureCatalogEntry(
            id: "pebblesnap",
            name: "Pebblesnap",
            description: "Flicks pebbles with surprising accuracy using its powerful tail.",
            habitatType: .dirt, rarity: .common, favoriteToy: "Slingshot",
            mutations: defaultMutations(colors: ["Brown", "Spotted Grey", "Sandy", "Dark Tan"])
        ),
        CreatureCatalogEntry(
            id: "moundmaker",
            name: "Moundmaker",
            description: "Constructs elaborate dirt mounds as homes and decoration.",
            habitatType: .dirt, rarity: .common, favoriteToy: "Architecture Kit",
            mutations: defaultMutations(colors: ["Earth Brown", "Red Soil", "Sand", "Clay Orange"])
        ),
        CreatureCatalogEntry(
            id: "tunnelworm",
            name: "Tunnelworm",
            description: "Can bore through solid packed earth in seconds, leaving smooth tunnels.",
            habitatType: .dirt, rarity: .uncommon, favoriteToy: "Tunnel Tube",
            mutations: defaultMutations(colors: ["Dark Brown", "Pink Worm", "Grey", "Pale Tan"])
        ),
        CreatureCatalogEntry(
            id: "quartzling",
            name: "Quartzling",
            description: "Grows small quartz crystals along its spine that chime softly when touched.",
            habitatType: .dirt, rarity: .rare, favoriteToy: "Crystal Chime",
            mutations: defaultMutations(colors: ["Rose Quartz", "Smoky Grey", "Clear Crystal", "Amethyst"])
        ),
        CreatureCatalogEntry(
            id: "geoheart",
            name: "Geoheart",
            description: "Its chest cavity contains a beating geode — its heart literally glitters.",
            habitatType: .dirt, rarity: .rare, favoriteToy: "Geode",
            mutations: defaultMutations(colors: ["Purple Geode", "Blue Crystal", "Pink Quartz", "Gold Vein"])
        ),
    ]

    // MARK: - Grass (15)

    static let grassCreatures: [CreatureCatalogEntry] = [
        CreatureCatalogEntry(
            id: "wildbloom",
            name: "Wildbloom",
            description: "Wherever it walks, wildflowers spontaneously bloom in its footprints.",
            habitatType: .grass, rarity: .common, favoriteToy: "Flower Press",
            mutations: defaultMutations(colors: ["Meadow Green", "Sunflower Yellow", "Violet", "Rose Pink"])
        ),
        CreatureCatalogEntry(
            id: "blossom",
            name: "Blossom",
            description: "A gentle creature with petals for ears that drift off in the breeze.",
            habitatType: .grass, rarity: .common, favoriteToy: "Petal Pile",
            mutations: defaultMutations(colors: ["Cherry Blossom", "White", "Lavender", "Peach"])
        ),
        CreatureCatalogEntry(
            id: "photosynthese",
            name: "Photosynthese",
            description: "Converts sunlight directly into energy — the happiest in bright habitats.",
            habitatType: .grass, rarity: .uncommon, favoriteToy: "Sunlamp",
            mutations: defaultMutations(colors: ["Bright Green", "Yellow Green", "Deep Forest", "Lime"])
        ),
        CreatureCatalogEntry(
            id: "chlorophyll",
            name: "Chlorophyll",
            description: "Can change its shade of green based on mood, from lime to deep forest.",
            habitatType: .grass, rarity: .common, favoriteToy: "Color Wheel",
            mutations: defaultMutations(colors: ["Lime Green", "Forest Green", "Sage", "Jade"])
        ),
        CreatureCatalogEntry(
            id: "vinetwist",
            name: "Vinetwist",
            description: "Long vines trail from its body, latching onto things with affectionate grip.",
            habitatType: .grass, rarity: .common, favoriteToy: "Climbing Frame",
            mutations: defaultMutations(colors: ["Leafy Green", "Brown Vine", "Purple Flower", "Yellow Bud"])
        ),
        CreatureCatalogEntry(
            id: "meadowpuff",
            name: "Meadowpuff",
            description: "A round, soft creature that rolls across meadows like a tumbleweed.",
            habitatType: .grass, rarity: .common, favoriteToy: "Ball",
            mutations: defaultMutations(colors: ["Cream White", "Soft Green", "Butter Yellow", "Sky Blue"])
        ),
        CreatureCatalogEntry(
            id: "thornback",
            name: "Thornback",
            description: "Defensive thorns cover its back, but it's the gentlest creature at heart.",
            habitatType: .grass, rarity: .uncommon, favoriteToy: "Soft Blanket",
            mutations: defaultMutations(colors: ["Dark Green", "Brown Thorn", "Red Berry", "Mossy"])
        ),
        CreatureCatalogEntry(
            id: "fernwhisper",
            name: "Fernwhisper",
            description: "Communicates by rustling ferns — an entire language of leaves.",
            habitatType: .grass, rarity: .uncommon, favoriteToy: "Fern Frond",
            mutations: defaultMutations(colors: ["Fern Green", "Light Olive", "Deep Teal", "Pale Yellow"])
        ),
        CreatureCatalogEntry(
            id: "seedling",
            name: "Seedling",
            description: "The youngest grass creature — constantly sprouting new features as it grows.",
            habitatType: .grass, rarity: .common, favoriteToy: "Watering Can",
            mutations: defaultMutations(colors: ["Spring Green", "Soft Brown", "Pale Yellow", "Mint"])
        ),
        CreatureCatalogEntry(
            id: "mossback",
            name: "Mossback",
            description: "Its back is covered in soft living moss that small insects call home.",
            habitatType: .grass, rarity: .common, favoriteToy: "Miniature Ecosystem",
            mutations: defaultMutations(colors: ["Moss Green", "Dark Brown", "Grey", "Bright Green"])
        ),
        CreatureCatalogEntry(
            id: "pollencloud",
            name: "Pollencloud",
            description: "Leaves a trail of golden pollen that makes nearby creatures inexplicably cheerful.",
            habitatType: .grass, rarity: .uncommon, favoriteToy: "Wind Toy",
            mutations: defaultMutations(colors: ["Golden Yellow", "Pale Orange", "Cream", "Honey"])
        ),
        CreatureCatalogEntry(
            id: "leafdancer",
            name: "Leafdancer",
            description: "Performs elaborate dances using fallen leaves as props and costumes.",
            habitatType: .grass, rarity: .uncommon, favoriteToy: "Leaf Collection",
            mutations: defaultMutations(colors: ["Autumn Orange", "Yellow", "Red", "Brown"])
        ),
        CreatureCatalogEntry(
            id: "rootweaver",
            name: "Rootweaver",
            description: "Weaves roots into intricate baskets and structures with surprising skill.",
            habitatType: .grass, rarity: .common, favoriteToy: "Weaving Kit",
            mutations: defaultMutations(colors: ["Root Brown", "Pale Wood", "Dark Earth", "Olive"])
        ),
        CreatureCatalogEntry(
            id: "sproutling",
            name: "Sproutling",
            description: "Has a small sprout growing from its head that blooms when extremely happy.",
            habitatType: .grass, rarity: .rare, favoriteToy: "Grow Kit",
            mutations: defaultMutations(colors: ["Fresh Green", "Pink Bloom", "White Blossom", "Sunny Yellow"])
        ),
        CreatureCatalogEntry(
            id: "verdantheart",
            name: "Verdantheart",
            description: "The rarest grass creature — its heart is a living seed that pulses with life force.",
            habitatType: .grass, rarity: .rare, favoriteToy: "Ancient Seed",
            mutations: defaultMutations(colors: ["Emerald", "Deep Forest", "Jade", "Golden Green"])
        ),
    ]

    // MARK: - Fire (10)

    static let fireCreatures: [CreatureCatalogEntry] = [
        CreatureCatalogEntry(
            id: "cinderborne",
            name: "Cinderborne",
            description: "Born from dying embers, it carries warmth wherever it wanders.",
            habitatType: .fire, rarity: .common, favoriteToy: "Ember Globe",
            mutations: defaultMutations(colors: ["Ash Grey", "Orange Ember", "Red Flame", "Deep Black"])
        ),
        CreatureCatalogEntry(
            id: "scorchwhirl",
            name: "Scorchwhirl",
            description: "Spins in tight circles, leaving scorched spiral patterns on the ground.",
            habitatType: .fire, rarity: .common, favoriteToy: "Spinning Wheel",
            mutations: defaultMutations(colors: ["Bright Orange", "Red", "Yellow", "Charcoal"])
        ),
        CreatureCatalogEntry(
            id: "flamewing",
            name: "Flamewing",
            description: "Its wings are made of living fire — beautiful but unapproachable to strangers.",
            habitatType: .fire, rarity: .uncommon, favoriteToy: "Feather Toy",
            mutations: defaultMutations(colors: ["Crimson", "Golden Flame", "Blue Fire", "White Hot"])
        ),
        CreatureCatalogEntry(
            id: "volatile",
            name: "Volatile",
            description: "Emotions translate directly to flame intensity — joy creates fireworks.",
            habitatType: .fire, rarity: .uncommon, favoriteToy: "Emotion Mirror",
            mutations: defaultMutations(colors: ["Explosive Red", "Bright Yellow", "Orange", "Purple Flame"])
        ),
        CreatureCatalogEntry(
            id: "emberpaw",
            name: "Emberpaw",
            description: "Leaves warm glowing paw prints that fade slowly after it walks past.",
            habitatType: .fire, rarity: .common, favoriteToy: "Paw Print Stamp",
            mutations: defaultMutations(colors: ["Orange", "Red", "Yellow Glow", "Deep Ember"])
        ),
        CreatureCatalogEntry(
            id: "sparksnout",
            name: "Sparksnout",
            description: "Its snout crackles with static and sparks with every sniff.",
            habitatType: .fire, rarity: .common, favoriteToy: "Static Ball",
            mutations: defaultMutations(colors: ["Electric Orange", "Yellow", "Red", "Gold"])
        ),
        CreatureCatalogEntry(
            id: "magmakin",
            name: "Magmakin",
            description: "Slow-moving but incredibly warm — perfect for heating a habitat in winter.",
            habitatType: .fire, rarity: .uncommon, favoriteToy: "Lava Lamp",
            mutations: defaultMutations(colors: ["Magma Red", "Dark Orange", "Black Rock", "Glowing Red"])
        ),
        CreatureCatalogEntry(
            id: "ashwalker",
            name: "Ashwalker",
            description: "Prefers cooler embers and ash over active flames — a contemplative fire type.",
            habitatType: .fire, rarity: .common, favoriteToy: "Ash Tray",
            mutations: defaultMutations(colors: ["Silver Ash", "Pale Grey", "White Ash", "Soft Black"])
        ),
        CreatureCatalogEntry(
            id: "solarflare",
            name: "Solarflare",
            description: "Channels solar energy, growing more powerful under bright light.",
            habitatType: .fire, rarity: .rare, favoriteToy: "Sun Prism",
            mutations: defaultMutations(colors: ["Solar Gold", "Bright White", "Orange Flame", "Deep Yellow"])
        ),
        CreatureCatalogEntry(
            id: "infernokin",
            name: "Infernokin",
            description: "The rarest fire creature, born only during meteor showers.",
            habitatType: .fire, rarity: .rare, favoriteToy: "Meteor Fragment",
            mutations: defaultMutations(colors: ["Deep Crimson", "Obsidian Black", "Bright Orange", "Starfire Blue"])
        ),
    ]

    // MARK: - Ice (10)

    static let iceCreatures: [CreatureCatalogEntry] = [
        CreatureCatalogEntry(
            id: "frostveil",
            name: "Frostveil",
            description: "Leaves a trail of delicate frost patterns on every surface it touches.",
            habitatType: .ice, rarity: .common, favoriteToy: "Snowflake Maker",
            mutations: defaultMutations(colors: ["Ice Blue", "Frost White", "Pale Silver", "Crystal Clear"])
        ),
        CreatureCatalogEntry(
            id: "frostbite",
            name: "Frostbite",
            description: "Playfully nips at things with teeth of solid ice — never truly harmful.",
            habitatType: .ice, rarity: .common, favoriteToy: "Chew Toy",
            mutations: defaultMutations(colors: ["Arctic White", "Ice Blue", "Pale Grey", "Teal Ice"])
        ),
        CreatureCatalogEntry(
            id: "tundraform",
            name: "Tundraform",
            description: "Can reshape the ice around itself into primitive structures and sculptures.",
            habitatType: .ice, rarity: .uncommon, favoriteToy: "Ice Mold",
            mutations: defaultMutations(colors: ["Glacier Blue", "Snow White", "Deep Ice", "Pale Teal"])
        ),
        CreatureCatalogEntry(
            id: "blizzardborne",
            name: "Blizzardborne",
            description: "Rides self-generated mini blizzards across the habitat at surprising speed.",
            habitatType: .ice, rarity: .uncommon, favoriteToy: "Snow Globe",
            mutations: defaultMutations(colors: ["Storm White", "Grey Blue", "Pale Violet", "Arctic Blue"])
        ),
        CreatureCatalogEntry(
            id: "snowpuff",
            name: "Snowpuff",
            description: "A perfectly spherical snow creature that bounces when excited.",
            habitatType: .ice, rarity: .common, favoriteToy: "Bounce Ball",
            mutations: defaultMutations(colors: ["Pure White", "Sky Blue", "Soft Grey", "Pale Pink"])
        ),
        CreatureCatalogEntry(
            id: "crystalmane",
            name: "Crystalmane",
            description: "Its mane is made of crystalline ice spines that catch light beautifully.",
            habitatType: .ice, rarity: .uncommon, favoriteToy: "Crystal Prism",
            mutations: defaultMutations(colors: ["Crystal Clear", "Ice Blue", "Pale Purple", "Silver"])
        ),
        CreatureCatalogEntry(
            id: "permafrost",
            name: "Permafrost",
            description: "Ancient and slow, it carries memories of the first winter in its icy core.",
            habitatType: .ice, rarity: .rare, favoriteToy: "Ancient Relic",
            mutations: defaultMutations(colors: ["Deep Ice Blue", "Grey", "Pale White", "Midnight Blue"])
        ),
        CreatureCatalogEntry(
            id: "hailstone",
            name: "Hailstone",
            description: "Launches perfectly round hailstones when startled — aims with surprising accuracy.",
            habitatType: .ice, rarity: .common, favoriteToy: "Target Board",
            mutations: defaultMutations(colors: ["Clear Ice", "White", "Blue Grey", "Frost Blue"])
        ),
        CreatureCatalogEntry(
            id: "glaciercalve",
            name: "Glaciercalve",
            description: "Splits off smaller versions of itself when very happy — they melt back by morning.",
            habitatType: .ice, rarity: .uncommon, favoriteToy: "Mirror",
            mutations: defaultMutations(colors: ["Glacier Blue", "White", "Pale Teal", "Ice Pink"])
        ),
        CreatureCatalogEntry(
            id: "aurorakin",
            name: "Aurorakin",
            description: "Emits cascading aurora-light from its fur at night, lighting the entire habitat.",
            habitatType: .ice, rarity: .rare, favoriteToy: "Light Prism",
            mutations: defaultMutations(colors: ["Aurora Green", "Purple", "Pink Aurora", "Blue Glow"])
        ),
    ]

    // MARK: - Electric (10)

    static let electricCreatures: [CreatureCatalogEntry] = [
        CreatureCatalogEntry(
            id: "sparkburst",
            name: "Sparkburst",
            description: "Releases involuntary sparks when surprised — like a living fireworks show.",
            habitatType: .electric, rarity: .common, favoriteToy: "Spark Stick",
            mutations: defaultMutations(colors: ["Electric Yellow", "White", "Blue Arc", "Orange Spark"])
        ),
        CreatureCatalogEntry(
            id: "voltspire",
            name: "Voltspire",
            description: "A spire of living electricity that loves conducting energy between objects.",
            habitatType: .electric, rarity: .uncommon, favoriteToy: "Conductor Rod",
            mutations: defaultMutations(colors: ["Bright Yellow", "Electric Blue", "White", "Purple Arc"])
        ),
        CreatureCatalogEntry(
            id: "electra",
            name: "Electra",
            description: "The original electric creature — ancient, dignified, crackling with power.",
            habitatType: .electric, rarity: .uncommon, favoriteToy: "Power Cell",
            mutations: defaultMutations(colors: ["Classic Yellow", "Silver", "Gold", "Blue White"])
        ),
        CreatureCatalogEntry(
            id: "luminant",
            name: "Luminant",
            description: "Glows steadily with stored electrical energy — a living nightlight.",
            habitatType: .electric, rarity: .common, favoriteToy: "Glow Orb",
            mutations: defaultMutations(colors: ["Warm Yellow", "Soft White", "Pale Blue", "Golden"])
        ),
        CreatureCatalogEntry(
            id: "thunderpup",
            name: "Thunderpup",
            description: "Young and exuberant, its tiny barks create miniature claps of thunder.",
            habitatType: .electric, rarity: .common, favoriteToy: "Thunder Drum",
            mutations: defaultMutations(colors: ["Sky Blue", "Yellow", "White", "Storm Grey"])
        ),
        CreatureCatalogEntry(
            id: "staticfur",
            name: "Staticfur",
            description: "Its fur stands permanently on end due to constant static charge.",
            habitatType: .electric, rarity: .common, favoriteToy: "Brush",
            mutations: defaultMutations(colors: ["Pale Yellow", "White Fluff", "Blue Tint", "Silver Grey"])
        ),
        CreatureCatalogEntry(
            id: "arcdancer",
            name: "Arcdancer",
            description: "Dances between electrical arcs with graceful, practiced precision.",
            habitatType: .electric, rarity: .uncommon, favoriteToy: "Dancing Ribbon",
            mutations: defaultMutations(colors: ["Electric Blue", "Purple Arc", "White", "Neon Yellow"])
        ),
        CreatureCatalogEntry(
            id: "stormcaller",
            name: "Stormcaller",
            description: "Predicts incoming storms hours early, growing agitated when pressure drops.",
            habitatType: .electric, rarity: .uncommon, favoriteToy: "Barometer",
            mutations: defaultMutations(colors: ["Storm Grey", "Dark Blue", "Electric Yellow", "Cloud White"])
        ),
        CreatureCatalogEntry(
            id: "thunderheart",
            name: "Thunderheart",
            description: "Its heartbeat is audible as soft thunder — a calming, rhythmic rumble.",
            habitatType: .electric, rarity: .rare, favoriteToy: "Metronome",
            mutations: defaultMutations(colors: ["Deep Blue", "Gold", "Storm White", "Purple"])
        ),
        CreatureCatalogEntry(
            id: "zenithstrike",
            name: "Zenithstrike",
            description: "The apex electric creature — its strike can be seen from miles away.",
            habitatType: .electric, rarity: .rare, favoriteToy: "Lightning Rod",
            mutations: defaultMutations(colors: ["Pure White", "Gold Strike", "Electric Blue", "Neon Green"])
        ),
    ]

    // MARK: - Magical (5, all Rare)

    static let magicalCreatures: [CreatureCatalogEntry] = [
        CreatureCatalogEntry(
            id: "arcane",
            name: "Arcane",
            description: "The embodiment of pure magic — its form shifts subtly with the phases of the moon.",
            habitatType: .magical, rarity: .rare, favoriteToy: "Moon Crystal",
            mutations: defaultMutations(colors: ["Mystic Purple", "Ethereal Blue", "Starlight Silver", "Deep Violet"])
        ),
        CreatureCatalogEntry(
            id: "constellation",
            name: "Constellation",
            description: "Stars map its body in real-time — it knows exactly what the night sky looks like.",
            habitatType: .magical, rarity: .rare, favoriteToy: "Star Chart",
            mutations: defaultMutations(colors: ["Midnight Blue", "Gold Stars", "Silver", "Deep Space"])
        ),
        CreatureCatalogEntry(
            id: "infinity",
            name: "Infinity",
            description: "Loops through reality in ways that defy explanation. Sometimes appears twice at once.",
            habitatType: .magical, rarity: .rare, favoriteToy: "Infinity Mirror",
            mutations: defaultMutations(colors: ["Void Black", "Neon Loop", "Rainbow", "Pure White"])
        ),
        CreatureCatalogEntry(
            id: "divinity",
            name: "Divinity",
            description: "The rarest of all creatures. Its presence alone makes nearby creatures calmer.",
            habitatType: .magical, rarity: .rare, favoriteToy: "Sacred Relic",
            mutations: defaultMutations(colors: ["Holy Gold", "White Light", "Soft Purple", "Celestial Blue"])
        ),
        CreatureCatalogEntry(
            id: "cosmicwarden",
            name: "Cosmicwarden",
            description: "Guardian of the magical realm. Appeared the day the ancient shop was founded.",
            habitatType: .magical, rarity: .rare, favoriteToy: "Cosmic Orb",
            mutations: defaultMutations(colors: ["Cosmic Purple", "Star Gold", "Deep Blue", "Ethereal Green"])
        ),
    ]

    // MARK: - Helpers

    private static func defaultMutations(colors: [String]) -> [MutationVariant] {
        colors.enumerated().map { MutationVariant(index: $0.offset, colorHint: $0.element) }
    }

    // MARK: - Lookup

    static func find(byID id: String) -> CreatureCatalogEntry? {
        allCreatures.first { $0.id == id }
    }

    static func creatures(ofType type: HabitatType) -> [CreatureCatalogEntry] {
        allCreatures.filter { $0.habitatType == type }
    }

    static func creatures(ofRarity rarity: Rarity) -> [CreatureCatalogEntry] {
        allCreatures.filter { $0.rarity == rarity }
    }
}
