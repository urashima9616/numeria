using System.Linq;
using Numeria.Core;

namespace Numeria.Game
{
    /// <summary>Passages derive solely from stable discovery IDs. No saved coordinates are rewritten.</summary>
    public static class AfterstoryJourney
    {
        public static bool Ready(MapDef def, Progress p) => def.GateCleared(p) ||
            def.Discoveries.Take(3).All(d => p.CollectedDiscoveries.Contains(d.Id));

        public static string Objective(MapDef def, Progress p)
        {
            if (def.GateCleared(p)) return def.Id == "ancient_ruins" ? "Your Mosaic Badge opens the lake." : "The Moonlight Badge is yours!";
            foreach (var d in def.Discoveries.Take(3))
                if (!p.CollectedDiscoveries.Contains(d.Id)) return "Restore the " + d.Name + ".";
            return "Meet " + def.BossDisplayName + ".";
        }

        public static void Apply(GridMap grid, MapDef def, Progress p)
        {
            if (!def.IsAfterstory) return;
            bool Solved(int index) => def.GateCleared(p) || p.CollectedDiscoveries.Contains(def.Discoveries[index].Id);
            void Passage(int index, int x, int y, int length, bool vertical, Tile open, Tile closed)
            {
                for (int i = 0; i < length; i++)
                    grid.SetTile(x + (vertical ? 0 : i), y + (vertical ? i : 0), Solved(index) ? open : closed);
            }
            if (def.Id == "ancient_ruins")
            {
                Passage(0, 14, 5, 3, false, Tile.Bridge, Tile.Water);
                Passage(1, 23, 11, 2, true, Tile.Path, Tile.Cliff);
                Passage(2, 14, 13, 3, false, Tile.Bridge, Tile.Water);
            }
            else
            {
                Passage(0, 7, 8, 4, false, Tile.Bridge, Tile.Water);
                Passage(1, 22, 8, 5, false, Tile.Bridge, Tile.Water);
                Passage(2, 16, 12, 3, true, Tile.Bridge, Tile.Water);
            }
        }
    }
}
