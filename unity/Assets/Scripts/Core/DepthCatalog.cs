using System;
using System.Collections.Generic;
using System.Linq;

namespace Numeria.Core
{
    public sealed class DepthSpecies
    {
        public string Id, Name, Element, Description;
        public bool Giant;
    }
    public sealed class DepthWorld
    {
        public string ParentId, Id, Name, Mechanic, Objective;
        public int Level;
        public DepthSpecies[] Species;
        public DepthSpecies Giant => Species.Last();
    }
    /// <summary>Eight optional depths: ten independent ordinary species and one collectible guardian each.</summary>
    public static class DepthCatalog
    {
        private static DepthSpecies Mon(string name,string element,string description,bool giant=false) =>
            new DepthSpecies {Id=name.ToLowerInvariant(),Name=name,Element=element,Description=description,Giant=giant};
        public static readonly DepthWorld[] Worlds =
        {
            new DepthWorld { ParentId="forest",Id="verdant_vault",Name="Verdant Vault",Level=12,Mechanic="Seed channels",Objective="Open seed channels to grow living bridges.",Species=new[] {
                Mon("Acornib","GRASS","It sorts acorns into tidy piles before sharing them with the forest."),
                Mon("Dewhopper","BUG","It hops between dewdrops and leaves tiny counting trails on leaves."),
                Mon("Fernfawn","GRASS","Its fern antlers unfurl one matching leaf on each side."),
                Mon("Mushmellow","FAIRY","This sleepy mushroom glows when nearby friends finish a pattern."),
                Mon("Rootter","EARTH","It builds root dams and checks that every stream gets a fair share."),
                Mon("Petalark","FLYING","Its petal wings scatter seeds in repeating colored rows."),
                Mon("Honeyhex","BUG","It fits six-sided honey rooms together without leaving any gaps."),
                Mon("Twiggle","GRASS","It collects crooked twigs and turns them into straight number paths."),
                Mon("Sprigsnail","WATER","Its spiral shell stores a cool drop for each thirsty seed."),
                Mon("Seedillo","EARTH","Its armored seed plates roll apart to make space for a new sprout."),
                Mon("Arboracle","GRASS","A gentle oak-backed tortoise shelters a whole garden beneath its canopy.",true),
            }},
            new DepthWorld { ParentId="mountains",Id="rime_gallery",Name="Rime Gallery",Level=22,Mechanic="Echo bridges",Objective="Repeat mountain echoes to raise frozen crossings.",Species=new[] {
                Mon("Snowmote","FAIRY","It drifts in matching snowflake pairs and never leaves a friend behind."),
                Mon("Rimehare","EARTH","It taps snowy steps in order before making its longest leap."),
                Mon("Icicub","WATER","Its ice paws leave little square prints along the frozen stream."),
                Mon("Chimegoat","ROCK","Its crystal horns ring a different note for each safe ledge."),
                Mon("Frostfinch","FLYING","It traces equal wings in the snow with every careful landing."),
                Mon("Glintmarmot","EARTH","It packs shiny stones into warm burrows in equal-sized groups."),
                Mon("Flurryfox","FAIRY","Its tail paints curling snow trails that always return to the start."),
                Mon("Quartzquill","ROCK","It counts its crystal quills to make sure none were lost in a tumble."),
                Mon("Sleetseal","WATER","It slides between ice circles and judges which route is shorter."),
                Mon("Peakpuff","SKY","This round cloud creature rests only on the tallest mountain cairns."),
                Mon("Glaciomamm","ROCK","A great woolly mammoth carries an entire blue glacier on its curved tusks.",true),
            }},
            new DepthWorld { ParentId="sky",Id="zephyr_gardens",Name="Zephyr Gardens",Level=30,Mechanic="Wind lanes",Objective="Turn wind vanes to connect floating islands.",Species=new[] {
                Mon("Kitelet","FLYING","Its kite-shaped tail points toward the next safe gust."),
                Mon("Cloudcalf","SKY","It gathers small clouds into fluffy groups before falling asleep."),
                Mon("Gustgecko","FLYING","Its wide toe pads cling to moving stones high above the clouds."),
                Mon("Nimbusquid","WATER","It swims through mist and ties raindrops into neat little loops."),
                Mon("Vanevulp","ELECTRIC","Its copper ears turn together whenever the wind changes direction."),
                Mon("Plumepup","FLYING","It balances on one feather and counts down before gliding."),
                Mon("Halohare","FAIRY","Its glowing ear rings make matching circles in the evening sky."),
                Mon("Drifturtle","SKY","It sails between islands on a shell shaped like a tiny airship."),
                Mon("Zephyray","FLYING","It measures gusts with the long ribbons at the tips of its wings."),
                Mon("Aerolotl","DRAGON","It puffs little cloud stairs so smaller friends can climb with it."),
                Mon("Stratocetus","SKY","An enormous sky whale carries floating gardens along the quiet upper winds.",true),
            }},
            new DepthWorld { ParentId="desert",Id="dusk_oasis",Name="Dusk Oasis",Level=42,Mechanic="Sun and moon",Objective="Switch sun and moon seals to reveal oasis paths.",Species=new[] {
                Mon("Sandib","EARTH","It digs cool shelters with entrances arranged in matching pairs."),
                Mon("Duneling","EARTH","Its sandy curls change shape when it rolls down a new dune."),
                Mon("Cactot","GRASS","It stores water in round pads and shares one pad at a time."),
                Mon("Miragecko","SKY","Its glassy scales reflect paths that disappear when viewed from the wrong side."),
                Mon("Scarabacus","BUG","It rolls bright beads along its shell like a living counting frame."),
                Mon("Suncamel","FIRE","Its glowing hump keeps travelers warm after the desert sun sets."),
                Mon("Oasisprout","WATER","It grows beside hidden wells and folds its leaves at noon."),
                Mon("Glasscobra","ROCK","Its clear hood catches sunlight and splits it into colored shapes."),
                Mon("Saffronfox","FIRE","It follows the shadow of its tail to find the coolest route home."),
                Mon("Moonjerboa","FAIRY","It wakes at dusk to trace star patterns with its long jumping feet."),
                Mon("Solarsphinx","EARTH","A giant sandstone sphinx shades the oasis beneath a crown shaped like a sundial.",true),
            }},
            new DepthWorld { ParentId="dark_mines",Id="resonant_foundry",Name="Resonant Foundry",Level=52,Mechanic="Power circuits",Objective="Link copper relays to light the abandoned foundry.",Species=new[] {
                Mon("Oremole","EARTH","It taps tunnel walls and listens for the richest echo."),
                Mon("Copperbat","FLYING","Its copper-lined wings carry sparks between distant cave lamps."),
                Mon("Geodillo","ROCK","It rolls up into a hollow geode when a tunnel gets noisy."),
                Mon("Voltweevil","ELECTRIC","It joins broken wires with the two tips of its glowing snout."),
                Mon("Coalcrab","FIRE","It carries warm coal in a shell that never burns its friends."),
                Mon("Amethystot","FAIRY","Its purple crystals pulse together when a circuit is complete."),
                Mon("Railrook","FLYING","It rides mine carts and calls out the order of each junction."),
                Mon("Nickelnewt","ELECTRIC","It slips through narrow seams and polishes them with its silver tail."),
                Mon("Lumenlug","BUG","Its lantern belly lights one square of the tunnel at a time."),
                Mon("Pyritoad","ROCK","It guards glittering fool's gold but happily trades for smooth pebbles."),
                Mon("Magnetaur","ELECTRIC","A towering iron bison pulls loose ore into orbit around its magnetic horns.",true),
            }},
            new DepthWorld { ParentId="underground",Id="ember_reservoir",Name="Ember Reservoir",Level=62,Mechanic="Cooling flow",Objective="Route cooling water to turn molten channels into stepping stones.",Species=new[] {
                Mon("Ashaxol","FIRE","Its feathery gills sift warm ash into soft piles."),
                Mon("Basaltot","ROCK","It stacks black stones into small towers beside cooling lava."),
                Mon("Cindercricket","BUG","It chirps a repeating rhythm whenever a vent is ready to cool."),
                Mon("Steamink","WATER","It draws temporary arrows in clouds of gentle steam."),
                Mon("Obsidianole","ROCK","Its glossy scales mirror the orange glow of underground rivers."),
                Mon("Magmole","EARTH","It digs side channels so hot streams never flood a friend's burrow."),
                Mon("Emberfin","FIRE","It swims through glowing pools without disturbing their calm surface."),
                Mon("Ventviper","FIRE","It curls around warm vents and releases heat in careful little breaths."),
                Mon("Pumipup","ROCK","Its airy stone paws let it float across warm pools."),
                Mon("Sootsprite","FAIRY","It sweeps soot into star shapes before the morning lamps are lit."),
                Mon("Calderadon","FIRE","A colossal basalt salamander carries a sleeping caldera along its broad back.",true),
            }},
            new DepthWorld { ParentId="ancient_ruins",Id="chronicle_archive",Name="Chronicle Archive",Level=74,Mechanic="Living mosaics",Objective="Restore mosaic keys in order to open the sealed archive.",Species=new[] {
                Mon("Glyphimp","MIND","It copies ancient marks and spots the one symbol that does not belong."),
                Mon("Tesselkid","ROCK","Its tiled coat rearranges itself into patterns with no empty spaces."),
                Mon("Papyrill","BUG","Its parchment wings preserve tiny stories from forgotten classrooms."),
                Mon("Cogcrab","EARTH","It walks sideways through old gears without missing a single beat."),
                Mon("Sundialynx","SKY","Its long shadow points to the safest doorway in the archive."),
                Mon("Mosaicarp","WATER","Its colored scales fit together like pieces of a river mosaic."),
                Mon("Abacat","MIND","It slides golden beads along its whiskers while watching the stars."),
                Mon("Relicub","ROCK","It carries broken statues to a quiet corner and patiently rebuilds them."),
                Mon("Scrollfinch","FLYING","It unrolls its tail feathers to show the route it flew yesterday."),
                Mon("Prismidol","FAIRY","It turns one shaft of light into a carefully ordered rainbow."),
                Mon("Chronogolem","MIND","A monumental clockwork archivist keeps lost stories safe inside a rotating stone library.",true),
            }},
            new DepthWorld { ParentId="veilmoon_lake",Id="moonwell_depths",Name="Moonwell Depths",Level=84,Mechanic="Moon tides",Objective="Tune moon mirrors to lower the tide and reveal hidden islands.",Species=new[] {
                Mon("Ripplewisp","FAIRY","It draws bright rings on still water without making a sound."),
                Mon("Moonotter","WATER","It shares round moonstones by making equal piles along the bank."),
                Mon("Lotusquid","WATER","Its petal arms open together when moonlight touches the lake."),
                Mon("Veilmoth","BUG","Its wings hide a matching pair of silver eyespots."),
                Mon("Pearlfin","WATER","It polishes one pearl for every new friend it meets."),
                Mon("Mistfawn","FAIRY","It steps between islands on paths made from low silver mist."),
                Mon("Reedrake","DRAGON","It weaves reeds into little bridges for creatures that cannot swim."),
                Mon("Tideshell","ROCK","Its shell records each rising tide with another pale ring."),
                Mon("Gloamray","SKY","It glides just above the lake and gathers reflected stars."),
                Mon("Starnewt","FAIRY","Its back lights up in constellations that change when the tide turns."),
                Mon("Lunabyss","WATER","An immense moonlit plesiosaur watches over a lake reflected inside its luminous crest.",true),
            }},
        };
        public static DepthWorld Find(string id) => Worlds.FirstOrDefault(w=>w.Id==id);
        public static DepthWorld ForParent(string id) => Worlds.FirstOrDefault(w=>w.ParentId==id);
        public static DepthSpecies Species(string id)
        {
            string first=ExpansionCatalog.DepthBase(id);
            return first==null?null:Worlds.SelectMany(w=>w.Species).FirstOrDefault(s=>s.Id==first);
        }
        public static IEnumerable<EvolutionLineDef> Lines() => Worlds.SelectMany(w=>w.Species).Select((s,i)=>new EvolutionLineDef
        { BaseId=s.Id,Element=s.Element,AffinityLabel=new[]{"number bonds","patterns","counting","symmetry","addition","equal groups"}[i%6],
          Affinity=new[]{PuzzleAffinity.MakeTen,PuzzleAffinity.Pattern,PuzzleAffinity.Counting,PuzzleAffinity.Symmetry,PuzzleAffinity.Formula,PuzzleAffinity.RepeatedAddition}[i%6],
          StageIds=ExpansionCatalog.DepthStages(s.Id),EvolutionLevels=EvolutionLevels(s) });
        private static int[] EvolutionLevels(DepthSpecies s)
        {
            int level=Worlds.First(w=>w.Species.Contains(s)).Level;
            return s.Giant?new[]{level+4,level+10}:new[]{level,level+8};
        }
        public static IEnumerable<SpeciesDef> Roster() => Worlds.SelectMany(w=>w.Species).Select((s,i)=>new SpeciesDef
        {Id=s.Id,Name=s.Name,MaxHp=s.Giant?22:12+i%5,AttackPower=s.Giant?5:3+i%3,DefensePower=s.Giant?4:2+i%3,
         HpGrowth=s.Giant?13:10+i%4,AttackGrowth=5,DefenseGrowth=4,BaseXp=s.Giant?28:12+i%6,
         SkillName=s.Name+" "+(s.Element=="WATER"?"Current":s.Element=="FIRE"?"Flare":s.Element=="ROCK"?"Pulse":"Echo"),
         SkillPower=s.Giant?8:5+i%3,Catchable=true,DropChance=.25,PreferredDrop=ConsumableType.HealthPotion});
    }
}
