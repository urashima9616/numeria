using System;

namespace Numeria.Core
{
    public static class TrainerSystem
    {
        public const double EncounterChance = .20;
        public static readonly string[] Ids = { "fern", "flint", "aster", "dune", "mica", "ember", "clio", "luna",
            "reed", "iris", "pip", "sage", "nora", "atlas", "opal", "cove", "basil", "nova", "wren", "sol" };
        public static string Name(string id) => string.IsNullOrEmpty(id) ? "Master" : char.ToUpperInvariant(id[0]) + id.Substring(1);
        public static string Portrait(string id) => "generated/Trainers/" + id;
        public static CombatantDef Roll(CombatantDef wild, Rng rng)
        {
            if (wild.IsBoss || !wild.Catchable || rng.Next() >= EncounterChance) return wild;
            return Boost(wild, Ids[rng.Pick(0, Ids.Length - 1)], rng.Pick(20, 40));
        }
        public static CombatantDef Boost(CombatantDef wild, string trainerId, int percent)
        {
            if (Array.IndexOf(Ids, trainerId) < 0) throw new ArgumentException("Unknown trainer", nameof(trainerId));
            if (wild == null || wild.IsBoss) throw new ArgumentException("Expected a wild encounter");
            int bonus = Math.Max(20, Math.Min(40, percent));
            int Stat(int value) => Math.Max(1, (int)Math.Round(value * (100 + bonus) / 100d, MidpointRounding.AwayFromZero));
            // Clone: never alter the wild baseline or species catalogue. Trainer partners are not catchable.
            return new CombatantDef { Id = wild.Id, Name = wild.Name, Level = wild.Level,
                MaxHp = Stat(wild.MaxHp), AttackPower = Stat(wild.AttackPower), DefensePower = Stat(wild.DefensePower),
                BaseXp = Stat(wild.BaseXp), DropChance = wild.DropChance, PreferredDrop = wild.PreferredDrop,
                Skills = wild.Skills, Catchable = false, IsBoss = false, Shield = null,
                TrainerId = trainerId, TrainerBonusPercent = bonus };
        }
    }
}
