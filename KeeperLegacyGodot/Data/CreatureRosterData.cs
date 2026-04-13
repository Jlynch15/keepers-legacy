// Data/CreatureRosterData.cs
// Port of CreatureRosterData.swift
// Static catalog of all 58 creatures across 7 habitat types.
// Pure C# — no Godot dependency.

using System.Collections.Generic;
using System.Linq;
using KeeperLegacy.Models;

namespace KeeperLegacy.Data
{
    public static class CreatureRosterData
    {
        // ── Full Catalog ──────────────────────────────────────────────────────

        // Initialized in the static constructor so all per-type lists exist first.
        public static readonly List<CreatureCatalogEntry> AllCreatures;

        static CreatureRosterData()
        {
            AllCreatures = WaterCreatures
                .Concat(DirtCreatures)
                .Concat(GrassCreatures)
                .Concat(FireCreatures)
                .Concat(IceCreatures)
                .Concat(ElectricCreatures)
                .Concat(MagicalCreatures)
                .ToList();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        public static CreatureCatalogEntry? Find(string id) =>
            AllCreatures.FirstOrDefault(c => c.Id == id);

        public static List<CreatureCatalogEntry> OfType(HabitatType type) =>
            AllCreatures.Where(c => c.HabitatType == type).ToList();

        public static List<CreatureCatalogEntry> OfRarity(Rarity rarity) =>
            AllCreatures.Where(c => c.Rarity == rarity).ToList();

        private static List<CreatureCatalogEntry.MutationVariant> Mutations(params string[] colors)
        {
            var list = new List<CreatureCatalogEntry.MutationVariant>();
            for (int i = 0; i < colors.Length; i++)
                list.Add(new CreatureCatalogEntry.MutationVariant(i, colors[i]));
            return list;
        }

        // ── Water (15) ────────────────────────────────────────────────────────

        public static readonly List<CreatureCatalogEntry> WaterCreatures = new()
        {
            new("aquaburst",     "Aquaburst",     "A bubbly water sprite that leaves sparkling trails wherever it swims.",               HabitatType.Water, Rarity.Common,   "Bubble Wand",       Mutations("Sapphire Blue",    "Teal Green",         "Pearl White",    "Coral Pink")),
            new("shimmerstream", "Shimmerstream", "Its scales catch light like a prism, casting rainbows across the habitat.",           HabitatType.Water, Rarity.Common,   "Shiny Pebble",      Mutations("Rainbow Silver",   "Deep Blue",          "Rose Gold",      "Midnight")),
            new("seraphine",     "Seraphine",     "A graceful creature with flowing fins that resemble angel wings.",                    HabitatType.Water, Rarity.Uncommon, "Silk Ribbon",       Mutations("Celestial White",  "Ocean Blue",         "Lavender",       "Gold")),
            new("deepecho",      "Deepecho",      "Born in the darkest ocean trenches, it communicates through haunting melodies.",      HabitatType.Water, Rarity.Uncommon, "Musical Shell",     Mutations("Abyss Black",      "Bioluminescent Blue","Deep Purple",    "Emerald")),
            new("tidecaller",    "Tidecaller",    "Can predict weather changes hours before they arrive.",                               HabitatType.Water, Rarity.Common,   "Driftwood",         Mutations("Storm Grey",       "Seafoam",            "Sandy Tan",      "Slate Blue")),
            new("coralsprite",   "Coralsprite",   "Lives among coral reefs and can grow tiny coral formations from its fingertips.",     HabitatType.Water, Rarity.Common,   "Coral Piece",       Mutations("Vivid Orange",     "Pink Coral",         "Pale Yellow",    "Purple")),
            new("mistwalker",    "Mistwalker",    "Glides across the water's surface, leaving a trail of morning mist.",                 HabitatType.Water, Rarity.Uncommon, "Fog Machine",       Mutations("Misty White",      "Grey Blue",          "Pale Green",     "Silver")),
            new("wavecrest",     "Wavecrest",     "Rides ocean waves with extraordinary skill and loves performing for crowds.",         HabitatType.Water, Rarity.Common,   "Surfboard Toy",     Mutations("Ocean Blue",       "Foam White",         "Sunset Orange",  "Turquoise")),
            new("pearlescent",   "Pearlescent",   "Its body glows with an inner pearlescent light that intensifies with happiness.",    HabitatType.Water, Rarity.Rare,     "Pearl Ball",        Mutations("Pure White",       "Blush Pink",         "Champagne",      "Iridescent")),
            new("riptide",       "Riptide",       "Fast and forceful, can generate powerful currents with a flick of its tail.",         HabitatType.Water, Rarity.Uncommon, "Speed Spinner",     Mutations("Electric Blue",    "Navy",               "Bright Teal",    "Storm White")),
            new("bubblesnout",   "Bubblesnout",   "Has an oversized snout that shoots bubbles in intricate patterns when excited.",      HabitatType.Water, Rarity.Common,   "Bubble Tube",       Mutations("Sky Blue",         "Soft Pink",          "Lime Green",     "Pale Yellow")),
            new("kelpling",      "Kelpling",      "Wears a cape of living kelp that sways even when perfectly still.",                  HabitatType.Water, Rarity.Common,   "Kelp Strand",       Mutations("Forest Green",     "Dark Blue",          "Brown",          "Golden Yellow")),
            new("frostfin",      "Frostfin",      "A cold-water denizen whose fins are tipped with permanent ice crystals.",             HabitatType.Water, Rarity.Uncommon, "Ice Cube",          Mutations("Ice Blue",         "Glacier White",      "Pale Purple",    "Crystal Clear")),
            new("swirlpool",     "Swirlpool",     "Creates miniature whirlpools for fun. Its body naturally spirals when resting.",      HabitatType.Water, Rarity.Common,   "Spinning Top",      Mutations("Turquoise",        "Deep Blue",          "Teal",           "Aquamarine")),
            new("luminara",      "Luminara",      "A rare deep-sea creature whose bioluminescence can light an entire room.",            HabitatType.Water, Rarity.Rare,     "Glow Stick",        Mutations("Electric Blue",    "Neon Green",         "Vivid Pink",     "Golden White")),
        };

        // ── Dirt (15) ─────────────────────────────────────────────────────────

        public static readonly List<CreatureCatalogEntry> DirtCreatures = new()
        {
            new("crumblebane",  "Crumblebane",  "Constantly shedding small pebbles it collects obsessively from the earth.",             HabitatType.Dirt, Rarity.Common,   "Pebble Collection", Mutations("Sandstone",       "Dark Brown",  "Red Clay",     "Ash Grey")),
            new("dustdevil",    "Dustdevil",    "Whips up tiny dust tornadoes when spinning in excitement.",                             HabitatType.Dirt, Rarity.Common,   "Pinwheel",          Mutations("Dust Brown",      "Tan",         "Russet",        "Pale Beige")),
            new("bedrock",      "Bedrock",      "The sturdiest of all dirt creatures — nothing can knock it over.",                      HabitatType.Dirt, Rarity.Uncommon, "Boulder Toy",       Mutations("Granite Grey",    "Dark Brown",  "Iron Black",   "Sandy Tan")),
            new("stonework",    "Stonework",    "Carves intricate patterns into nearby rocks using its hardened claws.",                 HabitatType.Dirt, Rarity.Uncommon, "Carving Tool",      Mutations("Marble White",    "Obsidian",    "Sandstone",    "Moss Green")),
            new("mudbubble",    "Mudbubble",    "Absolutely loves mud baths. The muddier, the happier.",                                HabitatType.Dirt, Rarity.Common,   "Mud Pie",           Mutations("Chocolate Brown", "Clay Red",    "Grey Mud",     "Pale Yellow")),
            new("terraclaw",    "Terraclaw",    "Its oversized claws are perfect for digging elaborate tunnel networks.",                HabitatType.Dirt, Rarity.Common,   "Shovel",            Mutations("Earth Brown",     "Orange Clay", "Dark Grey",    "Sand")),
            new("sandwhistle",  "Sandwhistle",  "Produces a haunting whistle by blowing air through porous sand-filled cavities.",       HabitatType.Dirt, Rarity.Uncommon, "Wind Chime",        Mutations("Desert Sand",     "Pale Orange", "Tan",          "Dune Gold")),
            new("rootbound",    "Rootbound",    "Has roots growing from its feet that anchor it during sleep.",                          HabitatType.Dirt, Rarity.Common,   "Seed Packet",       Mutations("Brown",           "Dark Root",   "Pale Wood",    "Mossy Green")),
            new("gravelgrip",   "Gravelgrip",   "Hands permanently coated in a layer of gravel it uses as natural armor.",              HabitatType.Dirt, Rarity.Common,   "Rock Tumbler",      Mutations("Gravel Grey",     "Slate",       "Brown",        "Black Pebble")),
            new("claymold",     "Claymold",     "Its body is malleable — it can reshape itself into rough sculptures.",                  HabitatType.Dirt, Rarity.Uncommon, "Clay Block",        Mutations("Red Clay",        "Grey",        "Terracotta",   "Dark Brown")),
            new("pebblesnap",   "Pebblesnap",   "Flicks pebbles with surprising accuracy using its powerful tail.",                     HabitatType.Dirt, Rarity.Common,   "Slingshot",         Mutations("Brown",           "Spotted Grey","Sandy",        "Dark Tan")),
            new("moundmaker",   "Moundmaker",   "Constructs elaborate dirt mounds as homes and decoration.",                            HabitatType.Dirt, Rarity.Common,   "Architecture Kit",  Mutations("Earth Brown",     "Red Soil",    "Sand",         "Clay Orange")),
            new("tunnelworm",   "Tunnelworm",   "Can bore through solid packed earth in seconds, leaving smooth tunnels.",              HabitatType.Dirt, Rarity.Uncommon, "Tunnel Tube",       Mutations("Dark Brown",      "Pink Worm",   "Grey",         "Pale Tan")),
            new("quartzling",   "Quartzling",   "Grows small quartz crystals along its spine that chime softly when touched.",          HabitatType.Dirt, Rarity.Rare,     "Crystal Chime",     Mutations("Rose Quartz",     "Smoky Grey",  "Clear Crystal","Amethyst")),
            new("geoheart",     "Geoheart",     "Its chest cavity contains a beating geode — its heart literally glitters.",            HabitatType.Dirt, Rarity.Rare,     "Geode",             Mutations("Purple Geode",    "Blue Crystal","Pink Quartz",  "Gold Vein")),
        };

        // ── Grass (15) ────────────────────────────────────────────────────────

        public static readonly List<CreatureCatalogEntry> GrassCreatures = new()
        {
            new("wildbloom",      "Wildbloom",      "Wherever it walks, wildflowers spontaneously bloom in its footprints.",                 HabitatType.Grass, Rarity.Common,   "Flower Press",        Mutations("Meadow Green",   "Sunflower Yellow","Violet",         "Rose Pink")),
            new("blossom",        "Blossom",        "A gentle creature with petals for ears that drift off in the breeze.",                  HabitatType.Grass, Rarity.Common,   "Petal Pile",          Mutations("Cherry Blossom", "White",           "Lavender",       "Peach")),
            new("photosynthese",  "Photosynthese",  "Converts sunlight directly into energy — the happiest in bright habitats.",             HabitatType.Grass, Rarity.Uncommon, "Sunlamp",             Mutations("Bright Green",   "Yellow Green",    "Deep Forest",    "Lime")),
            new("chlorophyll",    "Chlorophyll",    "Can change its shade of green based on mood, from lime to deep forest.",               HabitatType.Grass, Rarity.Common,   "Color Wheel",         Mutations("Lime Green",     "Forest Green",    "Sage",           "Jade")),
            new("vinetwist",      "Vinetwist",      "Long vines trail from its body, latching onto things with affectionate grip.",          HabitatType.Grass, Rarity.Common,   "Climbing Frame",      Mutations("Leafy Green",    "Brown Vine",      "Purple Flower",  "Yellow Bud")),
            new("meadowpuff",     "Meadowpuff",     "A round, soft creature that rolls across meadows like a tumbleweed.",                   HabitatType.Grass, Rarity.Common,   "Ball",                Mutations("Cream White",    "Soft Green",      "Butter Yellow",  "Sky Blue")),
            new("thornback",      "Thornback",      "Defensive thorns cover its back, but it's the gentlest creature at heart.",             HabitatType.Grass, Rarity.Uncommon, "Soft Blanket",        Mutations("Dark Green",     "Brown Thorn",     "Red Berry",      "Mossy")),
            new("fernwhisper",    "Fernwhisper",    "Communicates by rustling ferns — an entire language of leaves.",                        HabitatType.Grass, Rarity.Uncommon, "Fern Frond",          Mutations("Fern Green",     "Light Olive",     "Deep Teal",      "Pale Yellow")),
            new("seedling",       "Seedling",       "The youngest grass creature — constantly sprouting new features as it grows.",          HabitatType.Grass, Rarity.Common,   "Watering Can",        Mutations("Spring Green",   "Soft Brown",      "Pale Yellow",    "Mint")),
            new("mossback",       "Mossback",       "Its back is covered in soft living moss that small insects call home.",                 HabitatType.Grass, Rarity.Common,   "Miniature Ecosystem", Mutations("Moss Green",     "Dark Brown",      "Grey",           "Bright Green")),
            new("pollencloud",    "Pollencloud",    "Leaves a trail of golden pollen that makes nearby creatures inexplicably cheerful.",    HabitatType.Grass, Rarity.Uncommon, "Wind Toy",            Mutations("Golden Yellow",  "Pale Orange",     "Cream",          "Honey")),
            new("leafdancer",     "Leafdancer",     "Performs elaborate dances using fallen leaves as props and costumes.",                  HabitatType.Grass, Rarity.Uncommon, "Leaf Collection",     Mutations("Autumn Orange",  "Yellow",          "Red",            "Brown")),
            new("rootweaver",     "Rootweaver",     "Weaves roots into intricate baskets and structures with surprising skill.",             HabitatType.Grass, Rarity.Common,   "Weaving Kit",         Mutations("Root Brown",     "Pale Wood",       "Dark Earth",     "Olive")),
            new("sproutling",     "Sproutling",     "Has a small sprout growing from its head that blooms when extremely happy.",            HabitatType.Grass, Rarity.Rare,     "Grow Kit",            Mutations("Fresh Green",    "Pink Bloom",      "White Blossom",  "Sunny Yellow")),
            new("verdantheart",   "Verdantheart",   "The rarest grass creature — its heart is a living seed that pulses with life force.",   HabitatType.Grass, Rarity.Rare,     "Ancient Seed",        Mutations("Emerald",        "Deep Forest",     "Jade",           "Golden Green")),
        };

        // ── Fire (10) ─────────────────────────────────────────────────────────

        public static readonly List<CreatureCatalogEntry> FireCreatures = new()
        {
            new("cinderborne",  "Cinderborne",  "Born from dying embers, it carries warmth wherever it wanders.",                        HabitatType.Fire, Rarity.Common,   "Ember Globe",     Mutations("Ash Grey",       "Orange Ember", "Red Flame",    "Deep Black")),
            new("scorchwhirl",  "Scorchwhirl",  "Spins in tight circles, leaving scorched spiral patterns on the ground.",               HabitatType.Fire, Rarity.Common,   "Spinning Wheel",  Mutations("Bright Orange",  "Red",          "Yellow",       "Charcoal")),
            new("flamewing",    "Flamewing",    "Its wings are made of living fire — beautiful but unapproachable to strangers.",         HabitatType.Fire, Rarity.Uncommon, "Feather Toy",     Mutations("Crimson",        "Golden Flame", "Blue Fire",    "White Hot")),
            new("volatile",     "Volatile",     "Emotions translate directly to flame intensity — joy creates fireworks.",               HabitatType.Fire, Rarity.Uncommon, "Emotion Mirror",  Mutations("Explosive Red",  "Bright Yellow","Orange",       "Purple Flame")),
            new("emberpaw",     "Emberpaw",     "Leaves warm glowing paw prints that fade slowly after it walks past.",                  HabitatType.Fire, Rarity.Common,   "Paw Print Stamp", Mutations("Orange",         "Red",          "Yellow Glow",  "Deep Ember")),
            new("sparksnout",   "Sparksnout",   "Its snout crackles with static and sparks with every sniff.",                          HabitatType.Fire, Rarity.Common,   "Static Ball",     Mutations("Electric Orange","Yellow",        "Red",          "Gold")),
            new("magmakin",     "Magmakin",     "Slow-moving but incredibly warm — perfect for heating a habitat in winter.",            HabitatType.Fire, Rarity.Uncommon, "Lava Lamp",       Mutations("Magma Red",      "Dark Orange",  "Black Rock",   "Glowing Red")),
            new("ashwalker",    "Ashwalker",    "Prefers cooler embers and ash over active flames — a contemplative fire type.",         HabitatType.Fire, Rarity.Common,   "Ash Tray",        Mutations("Silver Ash",     "Pale Grey",    "White Ash",    "Soft Black")),
            new("solarflare",   "Solarflare",   "Channels solar energy, growing more powerful under bright light.",                     HabitatType.Fire, Rarity.Rare,     "Sun Prism",       Mutations("Solar Gold",     "Bright White", "Orange Flame", "Deep Yellow")),
            new("infernokin",   "Infernokin",   "The rarest fire creature, born only during meteor showers.",                           HabitatType.Fire, Rarity.Rare,     "Meteor Fragment", Mutations("Deep Crimson",   "Obsidian Black","Bright Orange","Starfire Blue")),
        };

        // ── Ice (10) ──────────────────────────────────────────────────────────

        public static readonly List<CreatureCatalogEntry> IceCreatures = new()
        {
            new("frostveil",      "Frostveil",      "Leaves a trail of delicate frost patterns on every surface it touches.",                HabitatType.Ice, Rarity.Common,   "Snowflake Maker", Mutations("Ice Blue",       "Frost White",  "Pale Silver",  "Crystal Clear")),
            new("frostbite",      "Frostbite",      "Playfully nips at things with teeth of solid ice — never truly harmful.",               HabitatType.Ice, Rarity.Common,   "Chew Toy",        Mutations("Arctic White",   "Ice Blue",     "Pale Grey",    "Teal Ice")),
            new("tundraform",     "Tundraform",     "Can reshape the ice around itself into primitive structures and sculptures.",            HabitatType.Ice, Rarity.Uncommon, "Ice Mold",        Mutations("Glacier Blue",   "Snow White",   "Deep Ice",     "Pale Teal")),
            new("blizzardborne",  "Blizzardborne",  "Rides self-generated mini blizzards across the habitat at surprising speed.",           HabitatType.Ice, Rarity.Uncommon, "Snow Globe",      Mutations("Storm White",    "Grey Blue",    "Pale Violet",  "Arctic Blue")),
            new("snowpuff",       "Snowpuff",       "A perfectly spherical snow creature that bounces when excited.",                         HabitatType.Ice, Rarity.Common,   "Bounce Ball",     Mutations("Pure White",     "Sky Blue",     "Soft Grey",    "Pale Pink")),
            new("crystalmane",    "Crystalmane",    "Its mane is made of crystalline ice spines that catch light beautifully.",               HabitatType.Ice, Rarity.Uncommon, "Crystal Prism",   Mutations("Crystal Clear",  "Ice Blue",     "Pale Purple",  "Silver")),
            new("permafrost",     "Permafrost",     "Ancient and slow, it carries memories of the first winter in its icy core.",            HabitatType.Ice, Rarity.Rare,     "Ancient Relic",   Mutations("Deep Ice Blue",  "Grey",         "Pale White",   "Midnight Blue")),
            new("hailstone",      "Hailstone",      "Launches perfectly round hailstones when startled — aims with surprising accuracy.",    HabitatType.Ice, Rarity.Common,   "Target Board",    Mutations("Clear Ice",      "White",        "Blue Grey",    "Frost Blue")),
            new("glaciercalve",   "Glaciercalve",   "Splits off smaller versions of itself when very happy — they melt back by morning.",    HabitatType.Ice, Rarity.Uncommon, "Mirror",          Mutations("Glacier Blue",   "White",        "Pale Teal",    "Ice Pink")),
            new("aurorakin",      "Aurorakin",      "Emits cascading aurora-light from its fur at night, lighting the entire habitat.",      HabitatType.Ice, Rarity.Rare,     "Light Prism",     Mutations("Aurora Green",   "Purple",       "Pink Aurora",  "Blue Glow")),
        };

        // ── Electric (10) ─────────────────────────────────────────────────────

        public static readonly List<CreatureCatalogEntry> ElectricCreatures = new()
        {
            new("sparkburst",   "Sparkburst",   "Releases involuntary sparks when surprised — like a living fireworks show.",              HabitatType.Electric, Rarity.Common,   "Spark Stick",    Mutations("Electric Yellow","White",         "Blue Arc",     "Orange Spark")),
            new("voltspire",    "Voltspire",    "A spire of living electricity that loves conducting energy between objects.",              HabitatType.Electric, Rarity.Uncommon, "Conductor Rod",  Mutations("Bright Yellow",  "Electric Blue","White",        "Purple Arc")),
            new("electra",      "Electra",      "The original electric creature — ancient, dignified, crackling with power.",              HabitatType.Electric, Rarity.Uncommon, "Power Cell",     Mutations("Classic Yellow", "Silver",       "Gold",         "Blue White")),
            new("luminant",     "Luminant",     "Glows steadily with stored electrical energy — a living nightlight.",                    HabitatType.Electric, Rarity.Common,   "Glow Orb",       Mutations("Warm Yellow",    "Soft White",   "Pale Blue",    "Golden")),
            new("thunderpup",   "Thunderpup",   "Young and exuberant, its tiny barks create miniature claps of thunder.",                 HabitatType.Electric, Rarity.Common,   "Thunder Drum",   Mutations("Sky Blue",       "Yellow",       "White",        "Storm Grey")),
            new("staticfur",    "Staticfur",    "Its fur stands permanently on end due to constant static charge.",                        HabitatType.Electric, Rarity.Common,   "Brush",          Mutations("Pale Yellow",    "White Fluff",  "Blue Tint",    "Silver Grey")),
            new("arcdancer",    "Arcdancer",    "Dances between electrical arcs with graceful, practiced precision.",                      HabitatType.Electric, Rarity.Uncommon, "Dancing Ribbon", Mutations("Electric Blue",  "Purple Arc",   "White",        "Neon Yellow")),
            new("stormcaller",  "Stormcaller",  "Predicts incoming storms hours early, growing agitated when pressure drops.",             HabitatType.Electric, Rarity.Uncommon, "Barometer",      Mutations("Storm Grey",     "Dark Blue",    "Electric Yellow","Cloud White")),
            new("thunderheart", "Thunderheart", "Its heartbeat is audible as soft thunder — a calming, rhythmic rumble.",                 HabitatType.Electric, Rarity.Rare,     "Metronome",      Mutations("Deep Blue",      "Gold",         "Storm White",  "Purple")),
            new("zenithstrike", "Zenithstrike", "The apex electric creature — its strike can be seen from miles away.",                    HabitatType.Electric, Rarity.Rare,     "Lightning Rod",  Mutations("Pure White",     "Gold Strike",  "Electric Blue","Neon Green")),
        };

        // ── Magical (5, all Rare) ─────────────────────────────────────────────

        public static readonly List<CreatureCatalogEntry> MagicalCreatures = new()
        {
            new("arcane",        "Arcane",        "The embodiment of pure magic — its form shifts subtly with the phases of the moon.",    HabitatType.Magical, Rarity.Rare, "Moon Crystal",   Mutations("Mystic Purple",  "Ethereal Blue",  "Starlight Silver","Deep Violet")),
            new("constellation", "Constellation", "Stars map its body in real-time — it knows exactly what the night sky looks like.",     HabitatType.Magical, Rarity.Rare, "Star Chart",     Mutations("Midnight Blue",  "Gold Stars",     "Silver",          "Deep Space")),
            new("infinity",      "Infinity",      "Loops through reality in ways that defy explanation. Sometimes appears twice at once.", HabitatType.Magical, Rarity.Rare, "Infinity Mirror",Mutations("Void Black",     "Neon Loop",      "Rainbow",         "Pure White")),
            new("divinity",      "Divinity",      "The rarest of all creatures. Its presence alone makes nearby creatures calmer.",        HabitatType.Magical, Rarity.Rare, "Sacred Relic",   Mutations("Holy Gold",      "White Light",    "Soft Purple",     "Celestial Blue")),
            new("cosmicwarden",  "Cosmicwarden",  "Guardian of the magical realm. Appeared the day the ancient shop was founded.",         HabitatType.Magical, Rarity.Rare, "Cosmic Orb",     Mutations("Cosmic Purple",  "Star Gold",      "Deep Blue",       "Ethereal Green")),
        };
    }
}
