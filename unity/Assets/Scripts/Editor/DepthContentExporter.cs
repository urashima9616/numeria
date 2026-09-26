using System.Collections.Generic;
using System.IO;
using System.Linq;
using Numeria.Core;
using Numeria.Game;
using UnityEditor;

namespace Numeria.Editor
{
    public static class DepthContentExporter
    {
        [MenuItem("Numeria/Export Depth Narration")]
        public static void Export()
        {
            var lines = new HashSet<string>();
            foreach(var mon in DepthCatalog.Worlds.SelectMany(w=>w.Species))
            {
                lines.Add($"A wild {mon.Name} appeared!");
                lines.Add($"Gotcha! {mon.Name} wants to travel with you!");
                lines.Add($"{mon.Name} is getting stronger!");
                lines.Add($"Level up! {mon.Name} is getting stronger!");
            }
            foreach(var species in GameData.Roster)
                foreach(var sentence in CollectionBook.Narration(species.Id)) lines.Add(sentence);
            foreach(var world in DepthCatalog.Worlds)
            {
                var map=Maps.Get(world.Id);lines.Add(map.WelcomeLine);lines.Add(world.Objective);
                lines.Add(map.BossLine);lines.Add(map.GateClearLine);
                lines.Add($"A wild Giant {world.Giant.Name} appeared! It has a pattern shield!");
                lines.Add($"You found {20+map.Tier*5} Numeria coins!");
                for(int stage=0;stage<3;stage++)
                    foreach(string line in new DepthMechanism(System.Array.IndexOf(DepthCatalog.Worlds,world),stage).Narration())lines.Add(line);
            }
            lines.Add("You restored my home. Now show me your team's courage!");
            lines.Add("Defeat this world's guardian to open its hidden depths.");
            lines.Add("Your giant friend is waiting in the Field Guide. Make room in your team to claim it!");
            File.WriteAllLines("../tools/depth-voice-lines.txt",lines.OrderBy(l=>l));
        }
    }
}
