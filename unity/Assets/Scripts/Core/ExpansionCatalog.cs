using System;
using System.Collections.Generic;
using System.Linq;

namespace Numeria.Core
{
    public sealed class HalloweenFamily
    {
        public string[] Names;
        public string Element, Skill, Description;
        public int Role;
        public string BaseId => Names[0].ToLowerInvariant();
        public string[] Ids => Names.Select(n => n.ToLowerInvariant()).ToArray();
    }

    /// <summary>Append-only content: legacy depth bases retain their original indexed stats.</summary>
    public static class ExpansionCatalog
    {
        private static readonly string[] DepthNames = {
            "Acornib|Acornock|Oakwarden",
            "Dewhopper|Dewleaper|Dewsymph",
            "Fernfawn|Fernhart|Frondstag",
            "Mushmellow|Shroomantle|Mycocrown",
            "Rootter|Brookroot|Deltotter",
            "Petalark|Bloomlark|Floraplume",
            "Honeyhex|Combkeeper|Hexahive",
            "Twiggle|Branchamble|Boughbinder",
            "Sprigsnail|Spiralsprig|Springhelix",
            "Seedillo|Podrillo|Germiguard",
            "Arboracle|Arborwarden|Worldarbor",
            "Snowmote|Flakeglimmer|Snowstellis",
            "Rimehare|Rimebound|Hoarfrosthare",
            "Icicub|Glazebear|Floeursa",
            "Chimegoat|Bellibex|Carillicorn",
            "Frostfinch|Snowquill|Borealfinch",
            "Glintmarmot|Gemmarmot|Cairnwarden",
            "Flurryfox|Snowtwirl|Auroravulp",
            "Quartzquill|Prismquill|Crystaspine",
            "Sleetseal|Floeseal|Glacierfin",
            "Peakpuff|Cirrocairn|Summitcloud",
            "Glaciomamm|Fjordmamm|Glacierarch",
            "Kitelet|Kiterove|Windbanner",
            "Cloudcalf|Cumulox|Nimbusire",
            "Gustgecko|Galegrip|Cyclogecko",
            "Nimbusquid|Mistmantle|Pluvioct",
            "Vanevulp|Coppervane|Tempestvulp",
            "Plumepup|Plumehound|Aeriehound",
            "Halohare|Ringbound|Aureohare",
            "Drifturtle|Sailturtle|Argoturtle",
            "Zephyray|Ribbonray|Jetstreamray",
            "Aerolotl|Aerobranch|Stratosolotl",
            "Stratocetus|Stratogarden|Celesticetus",
            "Sandib|Sandburrow|Duneguard",
            "Duneling|Dunetumble|Duneravel",
            "Cactot|Cactower|Cacthedral",
            "Miragecko|Miraglass|Miraguan",
            "Scarabacus|Beadscarab|Abacaraoh",
            "Suncamel|Emberhump|Solardrom",
            "Oasisprout|Wellsprig|Oasisbloom",
            "Glasscobra|Prismasp|Vitraserp",
            "Saffronfox|Saffrontail|Sunspice",
            "Moonjerboa|Moonbound|Astraboa",
            "Solarsphinx|Solarchon|Heliostela",
            "Oremole|Veinmole|Tunnelmarshal",
            "Copperbat|Bronzewing|Cupraven",
            "Geodillo|Geodoroll|Geodefort",
            "Voltweevil|Relayweevil|Circuitbeetle",
            "Coalcrab|Cokecarapace|Hearthclaw",
            "Amethystot|Amethyspark|Amethycrown",
            "Railrook|Switchrook|Junctionwing",
            "Nickelnewt|Nickelcrest|Ferronewt",
            "Lumenlug|Lumenweave|Beaconbeetle",
            "Pyritoad|Pyricroak|Auritoad",
            "Magnetaur|Polestaur|Magnetarch",
            "Ashaxol|Ashfrond|Cinerolotl",
            "Basaltot|Basaltier|Basaltower",
            "Cindercricket|Cinderchirp|Emberfiddle",
            "Steamink|Vaporink|Nebulink",
            "Obsidianole|Obsidrill|Obsidrake",
            "Magmole|Magmattock|Riftmole",
            "Emberfin|Emberwake|Cindermaw",
            "Ventviper|Ventcoil|Fumarasp",
            "Pumipup|Pumihound|Pumiguard",
            "Sootsprite|Sootswirl|Chimneyspark",
            "Calderadon|Caldercrest|Calderaegis",
            "Glyphimp|Glyphsayer|Glypharch",
            "Tesselkid|Tessello|Tessellord",
            "Papyrill|Papyrune|Papyrion",
            "Cogcrab|Gearclaw|Escapacrab",
            "Sundialynx|Shadowdial|Meridianlynx",
            "Mosaicarp|Mosaicfin|Tesselkoi",
            "Abacat|Beadlynx|Abacalion",
            "Relicub|Reliclaw|Reliquursa",
            "Scrollfinch|Scrollcrest|Codexwing",
            "Prismidol|Prismaltar|Prismoracle",
            "Chronogolem|Chronocurator|Epocholith",
            "Ripplewisp|Ripplehalo|Lacustria",
            "Moonotter|Pearlroot|Lunarotter",
            "Lotusquid|Lotusveil|Nelumara",
            "Veilmoth|Veilflutter|Argentmoth",
            "Pearlfin|Pearlcrest|Nacreon",
            "Mistfawn|Misthart|Veilstag",
            "Reedrake|Reedweaver|Marshwyrm",
            "Tideshell|Tidecarapace|Tidemonolith",
            "Gloamray|Gloamveil|Astraray",
            "Starnewt|Starfrond|Constellotl",
            "Lunabyss|Lunatide|Selenabyss",
        };
        private static readonly Dictionary<string,string[]> DepthLines = DepthNames
            .Select(row=>row.ToLowerInvariant().Split('|')).ToDictionary(row=>row[0]);
        private static readonly Dictionary<string,string> DepthBases = DepthLines.Values
            .SelectMany(row=>row.Select(id=>new {id,first=row[0]})).ToDictionary(x=>x.id,x=>x.first);
        private static readonly Dictionary<string,string> DepthDisplayNames = DepthNames
            .SelectMany(row=>row.Split('|')).ToDictionary(name=>name.ToLowerInvariant(),name=>name);
        public static string[] DepthStages(string id) => DepthLines[id];
        public static string DepthBase(string id) => id!=null&&DepthBases.TryGetValue(id,out var first)?first:null;
        public static string DepthName(string id) => id!=null&&DepthDisplayNames.TryGetValue(id,out var name)?name:null;

        private static HalloweenFamily F(string names, string element, int role, string skill, string description) =>
            new HalloweenFamily { Names = names.Split('|'), Element = element, Role = role, Skill = skill, Description = description };
        public static readonly HalloweenFamily[] Families = {
            F("Pipkin|Vinejack|Crownkin", "PUMPKIN", 2, "Harvest Crown", "Each seed it shares lights another lantern in the pumpkin patch."),
            F("Squashkit|Gourdpounce|Harvestlynx", "PUMPKIN", 0, "Rind Pounce", "It counts its pawprints before pouncing across the pumpkin rows."),
            F("Pumpup|Vinehound|Gourdmastiff", "PUMPKIN", 1, "Rind Ram", "It keeps every pumpkin safe inside a circle of curling vines."),
            F("Seedlet|Sproutimp|Patchsage", "PUMPKIN", 2, "Seed Array", "It plants seeds in tidy rows and leaves room for every sprout."),
            F("Gourdillo|Rindroller|Cucurguard", "PUMPKIN", 1, "Shell Roll", "Its pumpkin armor fits together without leaving a single gap."),
            F("Lanternib|Wickgourd|Beaconkin", "PUMPKIN", 2, "Lantern Beam", "It lights one lantern for every friend returning home."),
            F("Cobblekin|Stonegourd|Terrapump", "PUMPKIN", 1, "Terrace Pulse", "It carries a little garden on a shell made of pumpkin and stone."),
            F("Pulpuff|Creamkin|Piemancer", "PUMPKIN", 2, "Slice Swirl", "It shares its pumpkin pie so every friend gets a fair piece."),
            F("Vinebat|Gourdfang|Patchwing", "PUMPKIN", 0, "Vine Swoop", "Its leaf wings fold into two matching halves."),
            F("Hayhoot|Strawloft|Scareowl", "PUMPKIN", 0, "Straw Spiral", "It watches the fields and counts the bundles of golden straw."),
            F("Pumprib|Gourdcroak|Boglantern", "PUMPKIN", 1, "Pond Rings", "It makes gentle rings in the water with its glowing pumpkin throat."),
            F("Coilseed|Vineserp|Gourdaconda", "PUMPKIN", 0, "Coil Count", "It measures the garden paths with its long curling vine."),
            F("Stemstag|Rindbuck|Antlergourd", "PUMPKIN", 1, "Branch Charge", "Each branch on its antlers grows a matching leaf on the other side."),
            F("Spookernel|Hexhusk|Lanternmoth", "PUMPKIN", 0, "Petal Hex", "Its six glowing wing marks help travelers find the orchard."),
            F("Tallygourd|Abacush|Sumharvest", "PUMPKIN", 2, "Seed Sum", "It slides pumpkin seeds into groups before adding them together."),
            F("Wispip|Lanterngeist|Luminarch", "GHOST", 2, "Wisp Orbit", "It draws bright circles in the air to guide lost guests."),
            F("Sheetot|Drapewisp|Velourgeist", "GHOST", 1, "Curtain Sweep", "It folds its soft cloth wings into matching shapes."),
            F("Keyboo|Latchling|Lockwarden", "GHOST", 1, "Key Turn", "It knows which key shape fits each door in the house."),
            F("Tickaboo|Penduloom|Chronophantom", "GHOST", 2, "Midnight Chime", "It keeps time for the toys and helps every guest find the next dance."),
            F("Inklit|Quillshade|Scriptreign", "GHOST", 0, "Ink Arc", "It draws one bright mark for each story it remembers."),
            F("Dustmew|Veilcat|Phantomlynx", "GHOST", 0, "Silhouette Leap", "It follows moonlit footprints and always finds its way home."),
            F("Bellbit|Chimeraith|Carillord", "GHOST", 2, "Bell Echo", "Its bells ring in repeating patterns when friends arrive."),
            F("Mirrorimp|Reflecter|Speculord", "GHOST", 1, "Silver Fold", "It checks that every reflection has a matching partner."),
            F("Toyboo|Marionox|Puppetarch", "GHOST", 2, "Puppet Parade", "It teaches the toys to take their dance steps in order."),
            F("Candlepup|Wickwolf|Candelupus", "GHOST", 0, "Wick Rush", "It lights a row of candles with the tip of its little tail."),
            F("Pagepeek|Foliofloat|Tomewarden", "GHOST", 1, "Page Fan", "It finds missing pages and puts every story back in order."),
            F("Teacreep|Cupwisp|Porcelgeist", "GHOST", 2, "Tea Spiral", "It pours equal cups of warm tea for its ghostly guests."),
            F("Batboo|Echoveil|Sonarshade", "GHOST", 0, "Echo Sweep", "It follows returning echoes to find the shortest hallway."),
            F("Sockling|Mittenmist|Quiltguard", "GHOST", 1, "Patchwork Wave", "It sorts the lost mittens into pairs before bedtime."),
            F("Mousoleum|Cryptmouse|Vaultwhisk", "GHOST", 0, "Secret Passage", "It counts the little doors in its stone shelter."),
        };
        private static readonly Dictionary<string,HalloweenFamily> FamiliesByForm=Families
            .SelectMany(f=>f.Ids.Select(id=>new{id,f})).ToDictionary(x=>x.id,x=>x.f);
        public static HalloweenFamily Family(string id) => id!=null&&FamiliesByForm.TryGetValue(id,out var f)?f:null;
        public static string SkillIcon(string id)
        {
            var family=Family(id);int index=Array.IndexOf(Families,family);
            if(index<0)throw new ArgumentException("Unknown seasonal family",nameof(id));
            return "generated/Exploration/"+(index<15?"pumpkin":"ghost")+"-spell-"+(index%15);
        }
        public static SkillVisualKind Visual(string id)
        {
            var f=Family(id);int stage=Array.IndexOf(f.Ids,id);
            if(f.BaseId=="pipkin")return new[]{SkillVisualKind.SeedVolley,SkillVisualKind.VineLantern,SkillVisualKind.HarvestCrown}[stage];
            if(f.BaseId=="tickaboo")return new[]{SkillVisualKind.GhostOrbit,SkillVisualKind.PendulumArc,SkillVisualKind.MidnightChime}[stage];
            switch(f.BaseId)
            {
                case "squashkit":return SkillVisualKind.RindPounce;
                case "pumpup":return SkillVisualKind.RindRam;
                case "seedlet":return SkillVisualKind.SeedArray;
                case "gourdillo":return SkillVisualKind.ShellRoll;
                case "lanternib":return SkillVisualKind.LanternBeam;
                case "cobblekin":return SkillVisualKind.TerracePulse;
                case "pulpuff":return SkillVisualKind.SliceSwirl;
                case "vinebat":return SkillVisualKind.VineSwoop;
                case "hayhoot":return SkillVisualKind.StrawSpiral;
                case "pumprib":return SkillVisualKind.PondRings;
                case "coilseed":return SkillVisualKind.CoilCount;
                case "stemstag":return SkillVisualKind.BranchCharge;
                case "spookernel":return SkillVisualKind.PetalHex;
                case "tallygourd":return SkillVisualKind.SeedSum;
                case "wispip":return SkillVisualKind.WispOrbit;
                case "sheetot":return SkillVisualKind.CurtainSweep;
                case "keyboo":return SkillVisualKind.KeyTurn;
                case "inklit":return SkillVisualKind.InkArc;
                case "dustmew":return SkillVisualKind.SilhouetteLeap;
                case "bellbit":return SkillVisualKind.BellEcho;
                case "mirrorimp":return SkillVisualKind.SilverFold;
                case "toyboo":return SkillVisualKind.PuppetParade;
                case "candlepup":return SkillVisualKind.WickRush;
                case "pagepeek":return SkillVisualKind.PageFan;
                case "teacreep":return SkillVisualKind.TeaSpiral;
                case "batboo":return SkillVisualKind.EchoSweep;
                case "sockling":return SkillVisualKind.PatchworkWave;
                case "mousoleum":return SkillVisualKind.SecretPassage;
                default:throw new InvalidOperationException("Missing seasonal choreography: "+id);
            }
        }
        public static IEnumerable<EvolutionLineDef> Lines() => Families.Select((f, i) => new EvolutionLineDef {
            BaseId = f.BaseId, Element = f.Element, AffinityLabel = new[] {"addition", "number bonds", "patterns", "counting", "equal groups", "symmetry"}[i%6],
            Affinity = (PuzzleAffinity)(i%6), StageIds = f.Ids, EvolutionLevels = new[] {84, 92}
        });

        // Scale the full species curve, never captured offsets, equipment or training.
        public static IEnumerable<SpeciesDef> DepthEvolutions()
        {
            foreach (var original in DepthCatalog.Roster())
            {
                bool giant = DepthCatalog.Species(original.Id).Giant;
                var ids = DepthStages(original.Id);
                for (int stage = 1; stage <= 2; stage++)
                {
                    int hp = giant ? (stage == 1 ? 106 : 112) : (stage == 1 ? 112 : 125);
                    int stat = giant ? (stage == 1 ? 104 : 108) : (stage == 1 ? 110 : 120);
                    yield return new SpeciesDef {
                        Id=ids[stage], Name=DepthName(ids[stage]), MaxHp=original.MaxHp,
                        AttackPower=original.AttackPower, DefensePower=original.DefensePower,
                        HpGrowth=original.HpGrowth, AttackGrowth=original.AttackGrowth,
                        HpScalePercent=hp, CombatScalePercent=stat,
                        DefenseGrowth=original.DefenseGrowth, BaseXp=original.BaseXp+stage*4,
                        SkillName=DepthName(ids[stage])+" "+original.SkillName.Split(' ').Last(),
                        SkillPower=original.SkillPower+stage, Catchable=true, DropChance=original.DropChance,
                        PreferredDrop=original.PreferredDrop
                    };
                }
            }
        }
        public static IEnumerable<SpeciesDef> Roster()
        {
            foreach (var f in Families)
                for (int stage=0;stage<3;stage++)
                {
                    bool attack=f.Role==0, defense=f.Role==1;
                    yield return new SpeciesDef {
                        Id=f.Ids[stage],Name=f.Names[stage],MaxHp=(defense?18:15)+stage*5,
                        AttackPower=(attack?7:4)+stage*3, DefensePower=(defense?5:3)+stage*2,
                        // Flat subtraction damage makes DEF growth 4–5 cause 30+ turn mirror matches.
                        // Defensive families keep their identity through HP and a modest DEF advantage.
                        HpGrowth=defense?15:12, AttackGrowth=attack?6:5, DefenseGrowth=3,
                        BaseXp=18+stage*5,SkillName=f.BaseId=="pipkin"?new[]{"Seed Spark","Vine Lantern","Harvest Crown"}[stage]:
                            f.BaseId=="tickaboo"?new[]{"Tick Spark","Pendulum Arc","Midnight Chime"}[stage]:f.Skill,
                        SkillPower=5+stage,Catchable=true,DropChance=.25,PreferredDrop=ConsumableType.HealthPotion
                    };
                }
        }
        public static string Description(string id)
        {
            var family=Family(id);
            if(family?.BaseId=="pipkin") return new[]{
                "Pipkin shares its seeds with every new friend. Each seed lights a tiny lantern in the pumpkin patch.",
                "Vinejack carries lanterns on its curling branches. It counts each turn to guide lost friends home.",
                "Crownkin joins the orchard lights into a glowing crown. On harvest night, it makes sure every friend receives a fair share."
            }[Array.IndexOf(family.Ids,id)];
            if(family?.BaseId=="tickaboo") return new[]{
                "Tickaboo keeps a little pocket watch. Its gentle ticking reminds the toys when it is time to sleep.",
                "Penduloom swings its clock from side to side. The toys follow its steady beat to learn their dance steps.",
                "Chronophantom remembers every friend who has visited the house. At midnight, its three clock rings play a welcoming song."
            }[Array.IndexOf(family.Ids,id)];
            if(family!=null) return family.Description+" "+new[]{
                "Curious and playful, it is ready to explore with a friend.",
                "With practice, its magic has grown strong enough to help younger friends.",
                "A patient guardian, it uses its strongest magic to keep its home safe."
            }[Array.IndexOf(family.Ids,id)];
            var depth=DepthCatalog.Species(id);
            if(depth==null)return null;
            int stage=Array.IndexOf(DepthStages(depth.Id),id);
            return depth.Description+(stage<=0?"":stage==1?
                "Its growing magic now helps younger friends explore safely.":
                "Fully grown, it protects its home with the strongest form of its magic.");
        }
    }
}
