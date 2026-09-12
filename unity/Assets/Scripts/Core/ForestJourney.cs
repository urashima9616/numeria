using System.Collections.Generic;

namespace Numeria.Core
{
    /// <summary>Stable identities survive moving props. Old coordinate aliases stay recoverable.</summary>
    public static class ForestJourney
    {
        public const string Lanterns = "forest-rune-1";
        public const string Bridge = "forest-rune-2";
        public const string Mirror = "forest-rune-3";
        public static readonly Dictionary<string, string> ChestAliases = new Dictionary<string, string>
        {
            { "forest-chest-21-2", "forest-cache-riverbank" },
            { "forest-chest-24-4", "forest-cache-canopy" },
            { "forest-chest-26-8", "forest-cache-grove" },
            { "forest-chest-2-9", "forest-cache-roots" },
            { "forest-chest-2-14", "forest-cache-fireflies" },
        };

        public static string ChestId(string oldId) => ChestAliases.TryGetValue(oldId, out var id) ? id : oldId;
        public static bool BridgeRestored(Progress p) => p.BossBeaten || p.CollectedDiscoveries.Contains(Bridge);
        public static bool GuardianReady(Progress p) => p.BossBeaten ||
            (p.CollectedDiscoveries.Contains(Lanterns) && p.CollectedDiscoveries.Contains(Bridge) &&
             p.CollectedDiscoveries.Contains(Mirror));

        public static string Objective(Progress p)
        {
            if (p.BossBeaten) return "The mountain path is open!";
            if (!p.CollectedDiscoveries.Contains(Lanterns)) return "Wake the firefly lanterns.";
            if (!p.CollectedDiscoveries.Contains(Bridge)) return "Grow the sleeping vine bridge.";
            if (!p.CollectedDiscoveries.Contains(Mirror)) return "Restore the mirror grove.";
            return "Meet Numberfly at the ancient tree.";
        }

        public static void Migrate(Progress p)
        {
            bool allOldChests = true;
            foreach (var pair in ChestAliases)
            {
                bool opened = p.OpenedChests.Contains(pair.Key) || p.OpenedChests.Contains(pair.Value);
                allOldChests &= opened;
                if (opened && !p.OpenedChests.Contains(pair.Value)) p.OpenedChests.Add(pair.Value);
            }
            // A previously unlocked guardian must not be locked again after installing the new scene.
            if (p.BossBeaten || (p.SaveVersion < 10 && allOldChests))
                foreach (string id in new[] { Lanterns, Bridge, Mirror })
                    if (!p.CollectedDiscoveries.Contains(id)) p.CollectedDiscoveries.Add(id);
        }
    }
}
