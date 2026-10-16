using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Numeria.Core;
using Numeria.Game;
using UnityEngine;

namespace Numeria.Editor
{
    public static class HalloweenContentExporter
    {
        public static void Prepare()
        {
            HalloweenAssetImporter.Import();Export();
        }
        public static void Export()
        {
            var lines=new HashSet<string>();
            var families=GameData.Lines.Where(l=>DepthCatalog.Species(l.BaseId)!=null||ExpansionCatalog.Family(l.BaseId)!=null);
            foreach(var f in families)
            {
                foreach(string id in f.StageIds)
                {
                    string name=GameData.SpeciesById(id).Name;
                    foreach(string line in CollectionBook.Narration(id))lines.Add(line);
                    lines.Add($"A wild {name} appeared!");lines.Add($"Gotcha! {name} wants to travel with you!");
                    lines.Add($"{name} is getting stronger!");lines.Add($"Level up! {name} is getting stronger!");
                }
                for(int i=0;i<2;i++)
                {
                    string a=GameData.SpeciesById(f.StageIds[i]).Name,b=GameData.SpeciesById(f.StageIds[i+1]).Name;
                    lines.Add($"{a} is evolving!");lines.Add($"Amazing! {a} evolved into {b}!");
                    lines.Add($"Reach level {f.EvolutionLevels[i]} to evolve!");
                }
            }
            foreach(string id in HalloweenWorlds.Ids)
            {
                var m=Maps.Get(id);lines.UnionWith(new[]{m.WelcomeLine,m.BossLine,m.GateClearLine,m.GuardianVictoryLine,
                    "Meet "+m.BossDisplayName+".","A wild "+m.BossDisplayName+" appeared! It has a pattern shield!",
                    "A wild "+m.BossDisplayName+" appeared! It has a number shield!",
                    "Your new friends are waiting. Explore and complete your Field Guide!"});
                lines.UnionWith(m.GuardianChallengeLines);
                foreach(var d in m.Discoveries)lines.Add("Restore the "+d.Name+".");
            }
            for(int grade=0;grade<6;grade++)foreach(MathMode mode in Enum.GetValues(typeof(MathMode)))for(int kind=0;kind<6;kind++)for(uint seed=0;seed<30;seed++)
            {var settings=new MathSettings{Grade=grade};settings.Mode=mode;lines.UnionWith(new HalloweenMechanism(kind,settings,new Rng(seed)).Narration());}
            lines.Add("You found 75 Numeria coins!");lines.Add("You did it!");
            // Narration is composable, but this finite whole sentence must cover every valid random allocation, not only sampled seeds.
            for(int total=4;total<=18;total++)for(int count=2;count<=3;count++)
                lines.Add("There are "+total+" pumpkins and "+count+" baskets.");
            File.WriteAllLines("../tools/halloween-voice-lines.txt",lines.Where(l=>!string.IsNullOrEmpty(l)).OrderBy(l=>l));
            Debug.Log("Exported "+lines.Count+" Halloween narration lines; no player save accessed.");
        }
    }
}
